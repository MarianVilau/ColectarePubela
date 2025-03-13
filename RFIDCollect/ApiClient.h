bool sendTagData(const String &tagId) {
    // Check WiFi connection
    if (!isConnected()) {
        Serial.println("WiFi not connected!");
        return false;
    }

    HTTPClient http;

    Serial.print("Connecting to API...");
    if (!http.begin(client, API_ENDPOINT)) {
        Serial.println("Failed to connect to API endpoint");
        return false;
    }
    http.addHeader("Content-Type", "application/json");
    http.setTimeout(2000); // Set timeout to 2 seconds
    Serial.println("Connected to API");

    // Create JSON document
    StaticJsonDocument<200> jsonDoc;
    jsonDoc["Id"] = ID_COLECTARI;
    jsonDoc["IdPubela"] = tagId;
    jsonDoc["ColectedAdd"] = COLECTED_ADD;
    Serial.println("JSON document created");

    // Serialize JSON to string
    String jsonString;
    serializeJson(jsonDoc, jsonString);
    Serial.println("JSON serialized: " + jsonString);

    // Send POST request
    int httpResponseCode = http.POST(jsonString);
    Serial.println("POST request sent");

    if (httpResponseCode > 0) {
        String response = http.getString();
        Serial.println("HTTP Response code: " + String(httpResponseCode));
        Serial.println("Response: " + response);
        http.end();
        return true;
    } else {
        Serial.println("Error on sending POST: " + String(httpResponseCode));
        Serial.println("Error message: " + http.errorToString(httpResponseCode));
        http.end();
        return false;
    }
}
