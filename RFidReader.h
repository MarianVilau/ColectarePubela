#ifndef RFID_READER_H
#define RFID_READER_H

#include <SPI.h>
#include <MFRC522.h>
#include "defines.h"

class RfidReader {
private:
    MFRC522 rfid;
    String lastReadTagId;

    // Convert UID bytes to a string
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

public:
    // Constructor
    RfidReader() : rfid(SS_PIN, RST_PIN), lastReadTagId("") {}

    // Initialize the RFID reader
    void begin() {
        SPI.begin();
        rfid.PCD_Init();
        Serial.println("RFID Reader initialized");
        Serial.println("Waiting for tag...");
    }

    // Check for a new tag and return its ID if found (empty string if no new tag)
    String readTag() {
        // Look for new cards
        if (!rfid.PICC_IsNewCardPresent()) {
            return "";
        }

        // Select one of the cards
        if (!rfid.PICC_ReadCardSerial()) {
            return "";
        }

        // Get tag ID as string
        String tagId = getTagIdAsString(rfid.uid.uidByte, rfid.uid.size);

        // Print to serial monitor
        Serial.print("Tag ID: ");
        Serial.println(tagId);

        // Check if this is a new tag reading
        if (tagId == lastReadTagId) {
            // Halt PICC and stop encryption
            rfid.PICC_HaltA();
            rfid.PCD_StopCrypto1();
            return "";  // Skip duplicate readings
        }

        // Update last read tag
        lastReadTagId = tagId;

        // Halt PICC and stop encryption
        rfid.PICC_HaltA();
        rfid.PCD_StopCrypto1();

        return tagId;
    }

    // Reset the last read tag (useful after some time has passed)
    void resetLastTag() {
        lastReadTagId = "";
    }
};

#endif // RFID_READER_H