#ifndef NTP_CLIENT_H
#define NTP_CLIENT_H

#include <WiFi.h>
#include "time.h"

class NTPClient {
private:
    const char* ntpServer = "pool.ntp.org"; ///< NTP server address
    long gmtOffset_sec = 7200; ///< GMT offset in seconds
    int daylightOffset_sec = 3600; ///< Daylight saving time offset in seconds

public:
    /**
     * @brief Constructor for the NTPClient class.
     */
    NTPClient() {}

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
        strftime(timeString, sizeof(timeString), "%Y-%m-%dT%H:%M:%S", &timeinfo);
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
        strftime(timeString, sizeof(timeString), "%Y-%m-%dT%H:%M:%S", &timeinfo);
        Serial.println(timeString);
    }
};

#endif // NTP_CLIENT_H