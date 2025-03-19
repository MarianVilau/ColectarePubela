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
#include "time.h"

class ApiClient {
private:
    WiFiClient client; ///< WiFi client for HTTP communication
    const char* ntpServer = "pool.ntp.org"; ///< NTP server address
    long gmtOffset_sec = 0; ///< GMT offset in seconds
    int daylightOffset_sec = 3600; ///< Daylight saving time offset in seconds

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
        configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
        Serial.println("Time synchronization initialized");
    }

    /**
     * @brief Get the current time as a formatted string.
     * @return Formatted time string in ISO 8601 format
     */
    String getFormattedTime() {
        struct tm timeinfo;
        if (!getLocalTime(&timeinfo)) {
            Serial.println("Failed to obtain time");
            return "Failed to obtain time";
        }
        char timeString[25];
        strftime(timeString, sizeof(timeString), "%Y-%m-%dT%H:%M:%SZ", &timeinfo);
        return String(timeString);
    }

    /**
     * @brief Print detailed time information to the serial monitor.
     */
    void printLocalTime() {
        struct tm timeinfo;
        if (!getLocalTime(&timeinfo)) {
            Serial.println("Failed to obtain time");
            return;
        }
        char timeString[25];
        strftime(timeString, sizeof(timeString), "%Y-%m-%dT%H:%M:%SZ", &timeinfo);
        Serial.println(timeString);
    }

    /**
     * @brief Send RFID tag data to the API endpoint.
     * @param tagId The ID of the RFID tag to send.
     * @return True if the data was sent successfully, false otherwise.
     */
    bool sendTagData(const String &tagId) {
        if (!isConnected()) {
            Serial.println("Not connected to WiFi");
            return false;
        }

        HTTPClient http;
        String serverPath = String(API_ENDPOINT) + "/tags";
        http.begin(client, serverPath);

        http.addHeader("Content-Type", "application/json");

        DynamicJsonDocument jsonDoc(1024);
        jsonDoc["id_colectari"] = ID_COLECTARI;
        jsonDoc["tag_id"] = tagId;
        jsonDoc["timestamp"] = getFormattedTime();

        String requestBody;
        serializeJson(jsonDoc, requestBody);

        int httpResponseCode = http.POST(requestBody);

        if (httpResponseCode > 0) {
            String response = http.getString();
            Serial.println(httpResponseCode);
            Serial.println(response);
        } else {
            Serial.print("Error on sending POST: ");
            Serial.println(httpResponseCode);
        }

        http.end();
        return httpResponseCode == HTTP_CODE_OK;
    }
};

#endif // API_CLIENT_H