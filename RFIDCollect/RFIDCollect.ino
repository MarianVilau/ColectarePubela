#include "defines.h"
#include "RfidReader.h"
#include "ApiClient.h"

// Create instances
RfidReader rfidReader;
ApiClient apiClient;

// Timer variables
unsigned long lastTagResetTime = 0;
const unsigned long TAG_RESET_INTERVAL = 10000; // 10 seconds

// Timer for checking and sending offline tags
unsigned long lastOfflineCheckTime = 0;
const unsigned long OFFLINE_CHECK_INTERVAL = 30000; // 30 seconds

/**
 * @brief Setup function - called once at startup
 */
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

/**
 * @brief Main loop function - runs continuously
 */
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
  
  // Check and send stored offline tags if internet connection is available
  if (millis() - lastOfflineCheckTime > OFFLINE_CHECK_INTERVAL) {
    lastOfflineCheckTime = millis();
    
    // Display number of stored offline tags
    int offlineCount = apiClient.getOfflineTagCount();
    if (offlineCount > 0) {
      Serial.println("Stored offline tags: " + String(offlineCount));
    }
    
    // If there are offline tags and internet connection is available, try to send them
    if (offlineCount > 0 && apiClient.isConnected()) {
      Serial.println("Attempting to send offline tags...");
      int sentCount = apiClient.sendOfflineTags();
      
      if (sentCount > 0) {
        Serial.println("Sent " + String(sentCount) + " offline tags");
      } else {
        Serial.println("Could not send any offline tags");
      }
    }
  }
  
  // Short delay before next read
  delay(READ_DELAY);
}