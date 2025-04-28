#ifndef API_CLIENT_H
#define API_CLIENT_H

#ifdef ESP32
    #include <WiFi.h>
    #include <HTTPClient.h>
#else
    #include <ESP8266WiFi.h>
    #include <ESP8266HTTPClient.h>
#endif

#include <WiFiClient.h>
#include <ArduinoJson.h>
#include "defines.h"
#include "NTPClient.h"

// Max number of retry attempts and delay between retries
#define MAX_RETRY_ATTEMPTS 3
#define RETRY_DELAY_MS 2000
#define MAX_OFFLINE_TAGS 20 // Numărul maxim de tag-uri ce pot fi stocate în memoria flash

// Structura pentru stocarea datelor de tag offline
struct TagData {
    String tagId;
    String timestamp;
    bool valid; // Flag pentru a indica dacă locația conține date valide
};

class ApiClient {
private:
    WiFiClient client; ///< WiFi client for HTTP communication
    NTPClient ntpClient; ///< NTP client for time synchronization
    TagData offlineTags[MAX_OFFLINE_TAGS]; ///< Array pentru stocarea tag-urilor offline
    int offlineTagCount = 0; ///< Numărul actual de tag-uri stocate offline

public:
    /**
     * @brief Constructor for the ApiClient class.
     */
    ApiClient() {
        // Inițializează array-ul de tag-uri offline
        for (int i = 0; i < MAX_OFFLINE_TAGS; i++) {
            offlineTags[i].valid = false;
        }
    }

    /**
     * @brief Connect to the WiFi network.
     * @return True if connected successfully, false otherwise.
     */
    bool connectToWiFi() {
        WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
        Serial.print("Connecting to WiFi");

        // Wait for connection with timeout
        unsigned long startTime = millis();
        while (WiFi.status() != WL_CONNECTED) {
            delay(500);
            Serial.print(".");

            // Timeout after 20 seconds
            if (millis() - startTime > 20000) {
                Serial.println("\nWiFi connection timeout!");
                return false;
            }
        }

        Serial.println();
        Serial.println("Connected to WiFi");
        Serial.println("IP address: " + WiFi.localIP().toString());

        return true;
    }

    /**
     * @brief Check if the device is connected to WiFi.
     * @return True if connected, false otherwise.
     */
    bool isConnected() {
        return WiFi.status() == WL_CONNECTED;
    }

    /**
     * @brief Initialize time synchronization with the NTP server.
     */
    void initTime() {
        ntpClient.initTime();
    }

    /**
     * @brief Store tag data for offline use when WiFi is unavailable
     * @param tagId The ID of the RFID tag to store
     * @return True if the data was stored successfully, false if storage is full
     */
    bool storeOfflineTag(const String &tagId) {
        if (offlineTagCount >= MAX_OFFLINE_TAGS) {
            Serial.println("Storage is full, cannot store more offline tags");
            return false;
        }
        
        // Găsește primul loc disponibil în array
        for (int i = 0; i < MAX_OFFLINE_TAGS; i++) {
            if (!offlineTags[i].valid) {
                offlineTags[i].tagId = tagId;
                offlineTags[i].timestamp = ntpClient.getFormattedTime();
                offlineTags[i].valid = true;
                offlineTagCount++;
                
                Serial.println("Tag stocat offline: " + tagId);
                Serial.println("Total tag-uri offline: " + String(offlineTagCount));
                return true;
            }
        }
        
        return false;
    }

    /**
     * @brief Send stored offline tags to the API when WiFi is available
     * @return Number of successfully sent tags
     */
    int sendOfflineTags() {
        if (!isConnected()) {
            Serial.println("WiFi nu este conectat, nu se pot trimite tag-urile offline");
            return 0;
        }
        
        int sentCount = 0;
        
        for (int i = 0; i < MAX_OFFLINE_TAGS; i++) {
            if (offlineTags[i].valid) {
                if (sendTagData(offlineTags[i].tagId, offlineTags[i].timestamp)) {
                    // Eliberează locul după o trimitere reușită
                    offlineTags[i].valid = false;
                    offlineTagCount--;
                    sentCount++;
                    
                    Serial.println("Tag offline trimis cu succes: " + offlineTags[i].tagId);
                    
                    // Mică pauză între trimiteri pentru a evita suprasolicitarea API-ului
                    delay(500);
                } else {
                    // Dacă nu s-a reușit trimiterea, oprim procesul pentru a încerca din nou mai târziu
                    Serial.println("Eroare la trimiterea tag-ului offline, se va încerca din nou mai târziu");
                    break;
                }
            }
        }
        
        Serial.println("Tag-uri offline trimise: " + String(sentCount));
        Serial.println("Tag-uri offline rămase: " + String(offlineTagCount));
        
        return sentCount;
    }

    /**
     * @brief Get the number of stored offline tags
     * @return Number of stored offline tags
     */
    int getOfflineTagCount() {
        return offlineTagCount;
    }

    /**
     * @brief Send RFID tag data to the API endpoint with retry mechanism.
     * @param tagId The ID of the RFID tag to send.
     * @param maxRetries Maximum number of retry attempts (default: MAX_RETRY_ATTEMPTS).
     * @param retryDelay Delay between retry attempts in milliseconds (default: RETRY_DELAY_MS).
     * @return True if the data was sent successfully, false otherwise.
     */
    bool sendTagData(const String &tagId, int maxRetries = MAX_RETRY_ATTEMPTS, int retryDelay = RETRY_DELAY_MS) {
        // Check WiFi connection
        if (!isConnected()) {
            Serial.println("WiFi not connected!");
            // Store tag for offline use
            return storeOfflineTag(tagId);
        }

        // Prepare JSON data
        StaticJsonDocument<200> jsonDoc;
        jsonDoc["IdPubela"] = tagId;
        jsonDoc["CollectedAt"] = ntpClient.getFormattedTime();
        
        // Serialize JSON to string
        String jsonString;
        serializeJson(jsonDoc, jsonString);
        
        // Încercare inițială + reîncercări în caz de eșec
        for (int attempt = 1; attempt <= maxRetries; attempt++) {
            HTTPClient http;
            
            String serverPath = String(API_ENDPOINT);
            Serial.print("Încercarea #");
            Serial.print(attempt);
            Serial.print(" - Conectare la API: ");
            Serial.println(serverPath);

            if (!http.begin(client, serverPath)) {
                Serial.println("Eșec la conectarea la endpoint-ul API");
                delay(retryDelay);
                continue; // Treci la următoarea încercare
            }
            
            http.addHeader("Content-Type", "application/json");
            Serial.println("Conectat la API");
            Serial.println("JSON document creat");
            Serial.println("JSON serializat: " + jsonString);

            // Trimite cererea POST
            int httpResponseCode = http.POST(jsonString);
            Serial.println("Cerere POST trimisă");

            if (httpResponseCode > 0) {
                String response = http.getString();
                Serial.println("Cod de răspuns HTTP: " + String(httpResponseCode));
                Serial.println("Răspuns: " + response);
                http.end();
                return true; // Succes, ieșim din buclă
            } else {
                Serial.println("Eroare la trimiterea POST (Încercarea #" + String(attempt) + "): " + String(httpResponseCode));
                Serial.println("Mesaj de eroare (Încercarea #" + String(attempt) + "): " + http.errorToString(httpResponseCode));
                http.end();
                
                if (attempt < maxRetries) {
                    Serial.print("Se reîncearcă în ");
                    Serial.print(retryDelay / 1000.0);
                    Serial.println(" secunde...");
                    delay(retryDelay);
                } else {
                    Serial.println("S-au epuizat toate încercările. Eșec la trimiterea datelor.");
                    // Dacă toate încercările eșuează, stocăm tag-ul pentru trimitere ulterioară
                    return storeOfflineTag(tagId);
                }
            }
        }
        
        return false; // Toate încercările au eșuat
    }

    /**
     * @brief Send RFID tag data with specific timestamp to the API endpoint.
     * @param tagId The ID of the RFID tag to send.
     * @param timestamp The timestamp when the tag was collected.
     * @return True if the data was sent successfully, false otherwise.
     */
    bool sendTagData(const String &tagId, const String &timestamp, int maxRetries = MAX_RETRY_ATTEMPTS, int retryDelay = RETRY_DELAY_MS) {
        // Check WiFi connection
        if (!isConnected()) {
            Serial.println("WiFi not connected!");
            return false;
        }

        // Prepare JSON data
        StaticJsonDocument<200> jsonDoc;
        jsonDoc["IdPubela"] = tagId;
        jsonDoc["CollectedAt"] = timestamp;
        
        // Serialize JSON to string
        String jsonString;
        serializeJson(jsonDoc, jsonString);
        
        // Încercare inițială + reîncercări în caz de eșec
        for (int attempt = 1; attempt <= maxRetries; attempt++) {
            HTTPClient http;
            
            String serverPath = String(API_ENDPOINT);
            Serial.print("Încercarea #");
            Serial.print(attempt);
            Serial.print(" - Conectare la API: ");
            Serial.println(serverPath);

            if (!http.begin(client, serverPath)) {
                Serial.println("Eșec la conectarea la endpoint-ul API");
                delay(retryDelay);
                continue; // Treci la următoarea încercare
            }
            
            http.addHeader("Content-Type", "application/json");
            Serial.println("Conectat la API");
            Serial.println("JSON serializat: " + jsonString);

            // Trimite cererea POST
            int httpResponseCode = http.POST(jsonString);
            Serial.println("Cerere POST trimisă");

            if (httpResponseCode > 0) {
                String response = http.getString();
                Serial.println("Cod de răspuns HTTP: " + String(httpResponseCode));
                Serial.println("Răspuns: " + response);
                http.end();
                return true; // Succes, ieșim din buclă
            } else {
                Serial.println("Eroare la trimiterea POST (Încercarea #" + String(attempt) + "): " + String(httpResponseCode));
                Serial.println("Mesaj de eroare (Încercarea #" + String(attempt) + "): " + http.errorToString(httpResponseCode));
                http.end();
                
                if (attempt < maxRetries) {
                    Serial.print("Se reîncearcă în ");
                    Serial.print(retryDelay / 1000.0);
                    Serial.println(" secunde...");
                    delay(retryDelay);
                } else {
                    Serial.println("S-au epuizat toate încercările. Eșec la trimiterea datelor.");
                }
            }
        }
        
        return false; // Toate încercările au eșuat
    }
};

#endif // API_CLIENT_H