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

class ApiClient {
private:
    WiFiClient client; ///< WiFi client for HTTP communication
    NTPClient ntpClient; ///< NTP client for time synchronization

public:
    /**
     * @brief Constructor for the ApiClient class.
     */
    ApiClient() {}

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
            return false;
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
                Serial.println("Eroare la trimiterea POST: " + String(httpResponseCode));
                Serial.println("Mesaj de eroare: " + http.errorToString(httpResponseCode));
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