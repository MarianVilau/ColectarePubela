#include <SPI.h>
#include <MFRC522.h>
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <WiFiClient.h>
#include <ArduinoJson.h>

// WiFi credentials
const char* ssid = "OMiLAB";      // Replace with your WiFi SSID
const char* password = "digifofulbs";  // Replace with your WiFi password

// API endpoint
const char* apiEndpoint = "http://10.14.11.141:3000/api/data";

// Pin definitions based on the wiring diagram
#define RST_PIN D3  // GPIO0
#define SS_PIN  D4  // GPIO2

MFRC522 rfid(SS_PIN, RST_PIN);  // Create MFRC522 instance
String lastReadTagId = "";      // To avoid sending duplicate readings

void setup() {
  Serial.begin(9600);  // Initialize serial communication
  SPI.begin();         // Initialize SPI bus
  rfid.PCD_Init();     // Initialize RFID reader
  
  // Connect to WiFi
  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi");
  
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  
  Serial.println();
  Serial.println("Connected to WiFi");
  Serial.println("IP address: " + WiFi.localIP().toString());
  Serial.println("RFID Reader initialized");
  Serial.println("Waiting for tag...");
}

void loop() {
  // Look for new cards
  if (!rfid.PICC_IsNewCardPresent()) {
    return;
  }
  
  // Select one of the cards
  if (!rfid.PICC_ReadCardSerial()) {
    return;
  }
  
  // Get tag ID as string
  String tagId = getTagIdAsString(rfid.uid.uidByte, rfid.uid.size);
  
  // Print to serial monitor
  Serial.print("Tag ID: ");
  Serial.println(tagId);
  
  // Check if this is a new tag reading (avoid duplicates)
  if (tagId != lastReadTagId) {
    lastReadTagId = tagId;
    
    // Send data to API
    sendDataToAPI(tagId);
  }
  
  // Halt PICC
  rfid.PICC_HaltA();
  // Stop encryption on PCD
  rfid.PCD_StopCrypto1();
  
  delay(1000);  // Delay before next read
}

// Function to convert UID bytes to a string
String getTagIdAsString(byte *buffer, byte bufferSize) {
  String id = "";
  for (byte i = 0; i < bufferSize; i++) {
    if (buffer[i] < 0x10) {
      id += "0";
    }
    id += String(buffer[i], HEX);
  }
  id.toUpperCase();
  return id;
}

// Function to send data to the API
void sendDataToAPI(String tagId) {
  // Check WiFi connection
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi not connected!");
    return;
  }
  
  WiFiClient client;
  HTTPClient http;
  
  Serial.print("Connecting to API...");
  http.begin(client, apiEndpoint);
  http.addHeader("Content-Type", "application/json");
  
  // Create JSON document
  StaticJsonDocument<200> jsonDoc;
  jsonDoc["id"] = tagId;
  jsonDoc["name"] = "alt tag";  // Replace with actual name if available
  
  // Serialize JSON to string
  String jsonString;
  serializeJson(jsonDoc, jsonString);
  
  // Send POST request
  int httpResponseCode = http.POST(jsonString);
  
  if (httpResponseCode > 0) {
    String response = http.getString();
    Serial.println("HTTP Response code: " + String(httpResponseCode));
    Serial.println("Response: " + response);
  } else {
    Serial.println("Error on sending POST: " + String(httpResponseCode));
  }
  
  http.end();
}