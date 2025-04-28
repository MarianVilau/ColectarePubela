#include "defines.h"
#include "RfidReader.h"
#include "ApiClient.h"

// Create instances
RfidReader rfidReader;
ApiClient apiClient;

// Timer for resetting last tag
unsigned long lastTagResetTime = 0;
const unsigned long TAG_RESET_INTERVAL = 10000; // 10 seconds

// Timer pentru verificarea și trimiterea tag-urilor offline
unsigned long lastOfflineCheckTime = 0;
const unsigned long OFFLINE_CHECK_INTERVAL = 30000; // 30 secunde

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
  
  // Verifică și trimite tag-urile offline stocate, dacă conexiunea internet este disponibilă
  if (millis() - lastOfflineCheckTime > OFFLINE_CHECK_INTERVAL) {
    lastOfflineCheckTime = millis();
    
    // Afișează numărul de tag-uri offline stocate
    int offlineCount = apiClient.getOfflineTagCount();
    if (offlineCount > 0) {
      Serial.println("Tag-uri offline stocate: " + String(offlineCount));
    }
    
    // Dacă există tag-uri offline și conexiunea la internet este disponibilă, încearcă să le trimită
    if (offlineCount > 0 && apiClient.isConnected()) {
      Serial.println("Se încearcă trimiterea tag-urilor offline...");
      int sentCount = apiClient.sendOfflineTags();
      
      if (sentCount > 0) {
        Serial.println("S-au trimis " + String(sentCount) + " tag-uri offline");
      } else {
        Serial.println("Nu s-a putut trimite niciun tag offline");
      }
    }
  }
  
  // Short delay before next read
  delay(READ_DELAY);
}