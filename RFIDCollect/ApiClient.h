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
     * @brief Send RFID tag data to the API endpoint.
     * @param tagId The ID of the RFID tag to send.
     * @return True if the data was sent successfully, false otherwise.
     */
    bool sendTagData(const String &tagId) {
        // Check WiFi connection
        if (!isConnected()) {
            Serial.println("WiFi not connected!");
            return false;
        }

        HTTPClient http;

        String serverPath = String(API_ENDPOINT);
        Serial.print("Connecting to API at: ");
        Serial.println(serverPath);

        if (!http.begin(client, serverPath)) {
            Serial.println("Failed to connect to API endpoint");
            return false;
        }
        http.addHeader("Content-Type", "application/json");
        Serial.println("Connected to API");

        // Create JSON document
        StaticJsonDocument<200> jsonDoc;
        jsonDoc["Id"] = ID_COLECTARI;
        jsonDoc["IdPubela"] = tagId;
        jsonDoc["CollectedAt"] = ntpClient.getFormattedTime();
        Serial.println("JSON document created");

        // Serialize JSON to string
        String jsonString;
        serializeJson(jsonDoc, jsonString);
        Serial.println("JSON serialized: " + jsonString);

        // Send POST request
        int httpResponseCode = http.POST(jsonString);
        Serial.println("POST request sent");

        if (httpResponseCode > 0) {
            String response = http.getString();
            Serial.println("HTTP Response code: " + String(httpResponseCode));
            Serial.println("Response: " + response);
            http.end();
            return true;
        } else {
            Serial.println("Error on sending POST: " + String(httpResponseCode));
            Serial.println("Error message: " + http.errorToString(httpResponseCode));
            http.end();
            return false;
        }
    }
};

#endif // API_CLIENT_H