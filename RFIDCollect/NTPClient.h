/**
 * @file NTPClient.h
 * @brief NTP client class for synchronizing time with an NTP server.
 *
 * This class provides functionality to initialize time synchronization with an NTP server
 * and retrieve the current time in a formatted string.
 */

#ifndef NTP_CLIENT_H
#define NTP_CLIENT_H

#include "time.h"

/**
 * @class NTPClient
 * @brief Class for synchronizing time with an NTP server.
 */
public class NTPClient {
private:
    const char* ntpServer; ///< NTP server address
    long gmtOffset_sec; ///< GMT offset in seconds
    int daylightOffset_sec; ///< Daylight saving time offset in seconds

public:
    /**
     * @brief Constructor for the NTPClient class.
     * @param ntpServer NTP server address
     * @param gmtOffset_sec GMT offset in seconds
     * @param daylightOffset_sec Daylight saving time offset in seconds
     */
    NTPClient(const char* ntpServer, long gmtOffset_sec, int daylightOffset_sec)
            : ntpServer(ntpServer), gmtOffset_sec(gmtOffset_sec), daylightOffset_sec(daylightOffset_sec) {}

    /**
     * @brief Initialize time synchronization with the NTP server.
     */
    void initTime() {
        configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
    }

    /**
     * @brief Get the current time as a formatted string.
     * @return Formatted time string in ISO 8601 format
     */
    String getFormattedTime() {
        struct tm timeinfo;
        if (!getLocalTime(&timeinfo)) {
            return "Failed to obtain time";
        }
        char timeString[20];
        strftime(timeString, sizeof(timeString), "%Y-%m-%dT%H:%M:%S", &timeinfo);
        return String(timeString);
    }
};

#endif // NTP_CLIENT_H