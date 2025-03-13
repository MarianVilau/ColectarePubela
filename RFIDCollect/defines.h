#ifndef DEFINES_H
#define DEFINES_H

#include "WifiConfig.h"

// API configuration
#define ID_COLECTARI "2"
#define COLECTED_ADD "2023-10-10T10:00:00"

// Pin definitions
#ifdef ESP32
  #define RST_PIN 21  // GPIO21
  #define SS_PIN  5   // GPIO05
#elif defined(ESP8266)
  #define RST_PIN D3  // GPIO0
  #define SS_PIN  D4  // GPIO2
#endif

// Other constants
#define SERIAL_BAUD_RATE 9600
#define READ_DELAY 1000  // Delay between reads in milliseconds

#endif // DEFINES_H