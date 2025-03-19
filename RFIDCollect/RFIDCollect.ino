#include "defines.h"
#include "RfidReader.h"
#include "ApiClient.h"

// Create instances
RfidReader rfidReader;
ApiClient apiClient;

// Timer for resetting last tag
unsigned long lastTagResetTime = 0;
const unsigned long TAG_RESET_INTERVAL = 10000; // 10 seconds

void setup() {
  // Initialize serial communication
  Serial.begin(SERIAL_BAUD_RATE);
  Serial.println("\n\nRFID to API Integration");
  Serial.println("------------------------");
  
  // Connect to WiFi
  if (!apiClient.connectToWiFi()) {
    Serial.println("Failed to connect to WiFi. Check credentials or router.");
  }
  //Initialize NTP client
  apiClient.initTime();
  // Initialize RFID reader
  rfidReader.begin();
}

void loop() {
  // Try to reconnect if WiFi connection is lost
  if (!apiClient.isConnected()) {
    Serial.println("WiFi connection lost. Trying to reconnect...");
    apiClient.connectToWiFi();
  }
  
  // Read RFID tag
  String tagId = rfidReader.readTag();
  
  // If a tag was read, send to API
  if (tagId.length() > 0) {
    if (apiClient.sendTagData(tagId)) {
      Serial.println("Data successfully sent to API!");
    } else {
      Serial.println("Failed to send data to API.");
    }
  }
  
  // Reset last tag after some interval to allow re-reading the same tag
  if (millis() - lastTagResetTime > TAG_RESET_INTERVAL) {
    rfidReader.resetLastTag();
    lastTagResetTime = millis();
  }
  
  // Short delay before next read
  delay(READ_DELAY);
}