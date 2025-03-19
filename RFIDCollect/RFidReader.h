/**
 * @file RFidReader.h
 * @brief RFID reader class for reading RFID tags.
 *
 * This class provides functionality to initialize the RFID reader, read RFID tags, and reset the last read tag.
 */

#ifndef RFID_READER_H
#define RFID_READER_H

#include <SPI.h>
#include <MFRC522.h>
#include "defines.h"

/**
 * @class RfidReader
 * @brief Class for reading RFID tags using the MFRC522 module.
 */
class RfidReader {
private:
    MFRC522 rfid; ///< MFRC522 instance for RFID communication
    String lastReadTagId; ///< Stores the last read tag ID

    /**
     * @brief Convert UID bytes to a string.
     * @param buffer Pointer to the UID buffer
     * @param bufferSize Size of the UID buffer
     * @return String representation of the UID
     */
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
    /**
     * @brief Constructor for the RfidReader class.
     */
    RfidReader() : rfid(SS_PIN, RST_PIN), lastReadTagId("") {}

    /**
     * @brief Initialize the RFID reader.
     *
     * This function initializes the SPI communication and the MFRC522 module.
     */
    void begin() {
        SPI.begin();
        rfid.PCD_Init();
        Serial.println("RFID Reader initialized");
        Serial.println("Waiting for tag...");
    }

    /**
     * @brief Check for a new tag and return its ID if found.
     * @return String representation of the tag ID (empty string if no new tag)
     */
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

    /**
     * @brief Reset the last read tag.
     *
     * This function resets the last read tag ID, allowing the same tag to be read again.
     */
    void resetLastTag() {
        lastReadTagId = "";
    }
};

#endif // RFID_READER_H