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
#define MAX_OFFLINE_TAGS 20 // Maximum number of tags that can be stored in memory

/**
 * @brief Structure for storing offline tag data
 */
struct TagData {
    String tagId;      ///< The RFID tag identifier
    String timestamp;  ///< The timestamp when the tag was read
    bool valid;        ///< Flag indicating if this slot contains valid data
};

class ApiClient {
private:
    WiFiClient client; ///< WiFi client for HTTP communication
    NTPClient ntpClient; ///< NTP client for time synchronization
    TagData offlineTags[MAX_OFFLINE_TAGS]; ///< Array for storing offline tags
    int offlineTagCount = 0; ///< Current number of stored offline tags

public:
    /**
     * @brief Constructor for the ApiClient class.
     */
    ApiClient() {
        // Initialize offline tags array
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
        
        // Find first available slot in the array
        for (int i = 0; i < MAX_OFFLINE_TAGS; i++) {
            if (!offlineTags[i].valid) {
                offlineTags[i].tagId = tagId;
                offlineTags[i].timestamp = ntpClient.getFormattedTime();
                offlineTags[i].valid = true;
                offlineTagCount++;
                
                Serial.println("Tag stored offline: " + tagId);
                Serial.println("Total offline tags: " + String(offlineTagCount));
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
            Serial.println("WiFi not connected, cannot send offline tags");
            return 0;
        }
        
        int sentCount = 0;
        
        for (int i = 0; i < MAX_OFFLINE_TAGS; i++) {
            if (offlineTags[i].valid) {
                if (sendTagData(offlineTags[i].tagId, offlineTags[i].timestamp)) {
                    // Free slot after successful send
                    offlineTags[i].valid = false;
                    offlineTagCount--;
                    sentCount++;
                    
                    Serial.println("Offline tag sent successfully: " + offlineTags[i].tagId);
                    
                    // Small delay between sends to avoid overwhelming the API
                    delay(500);
                } else {
                    // If sending fails, stop the process to try again later
                    Serial.println("Error sending offline tag, will try again later");
                    break;
                }
            }
        }
        
        Serial.println("Offline tags sent: " + String(sentCount));
        Serial.println("Offline tags remaining: " + String(offlineTagCount));
        
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
        
        // Initial attempt + retries in case of failure
        for (int attempt = 1; attempt <= maxRetries; attempt++) {
            HTTPClient http;
            
            String serverPath = String(API_ENDPOINT);
            Serial.print("Attempt #");
            Serial.print(attempt);
            Serial.print(" - Connecting to API: ");
            Serial.println(serverPath);

            if (!http.begin(client, serverPath)) {
                Serial.println("Failed to connect to API endpoint");
                delay(retryDelay);
                continue; // Move to next attempt
            }
            
            http.addHeader("Content-Type", "application/json");
            Serial.println("Connected to API");
            Serial.println("JSON document created");
            Serial.println("JSON serialized: " + jsonString);

            // Send POST request
            int httpResponseCode = http.POST(jsonString);
            Serial.println("POST request sent");

            if (httpResponseCode > 0) {
                String response = http.getString();
                Serial.println("HTTP response code: " + String(httpResponseCode));
                Serial.println("Response: " + response);
                http.end();
                return true; // Success, exit loop
            } else {
                Serial.println("Error sending POST (Attempt #" + String(attempt) + "): " + String(httpResponseCode));
                Serial.println("Error message (Attempt #" + String(attempt) + "): " + http.errorToString(httpResponseCode));
                http.end();
                
                if (attempt < maxRetries) {
                    Serial.print("Retrying in ");
                    Serial.print(retryDelay / 1000.0);
                    Serial.println(" seconds...");
                    delay(retryDelay);
                } else {
                    Serial.println("All attempts exhausted. Failed to send data.");
                    // If all attempts fail, store the tag for later sending
                    return storeOfflineTag(tagId);
                }
            }
        }
        
        return false; // All attempts failed
    }

    /**
     * @brief Send RFID tag data with specific timestamp to the API endpoint.
     * @param tagId The ID of the RFID tag to send.
     * @param timestamp The timestamp when the tag was collected.
     * @param maxRetries Maximum number of retry attempts (default: MAX_RETRY_ATTEMPTS).
     * @param retryDelay Delay between retry attempts in milliseconds (default: RETRY_DELAY_MS).
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
        
        // Initial attempt + retries in case of failure
        for (int attempt = 1; attempt <= maxRetries; attempt++) {
            HTTPClient http;
            
            String serverPath = String(API_ENDPOINT);
            Serial.print("Attempt #");
            Serial.print(attempt);
            Serial.print(" - Connecting to API: ");
            Serial.println(serverPath);

            if (!http.begin(client, serverPath)) {
                Serial.println("Failed to connect to API endpoint");
                delay(retryDelay);
                continue; // Move to next attempt
            }
            
            http.addHeader("Content-Type", "application/json");
            Serial.println("Connected to API");
            Serial.println("JSON serialized: " + jsonString);

            // Send POST request
            int httpResponseCode = http.POST(jsonString);
            Serial.println("POST request sent");

            if (httpResponseCode > 0) {
                String response = http.getString();
                Serial.println("HTTP response code: " + String(httpResponseCode));
                Serial.println("Response: " + response);
                http.end();
                return true; // Success, exit loop
            } else {
                Serial.println("Error sending POST (Attempt #" + String(attempt) + "): " + String(httpResponseCode));
                Serial.println("Error message (Attempt #" + String(attempt) + "): " + http.errorToString(httpResponseCode));
                http.end();
                
                if (attempt < maxRetries) {
                    Serial.print("Retrying in ");
                    Serial.print(retryDelay / 1000.0);
                    Serial.println(" seconds...");
                    delay(retryDelay);
                } else {
                    Serial.println("All attempts exhausted. Failed to send data.");
                }
            }
        }
        
        return false; // All attempts failed
    }
};

#endif // API_CLIENT_H