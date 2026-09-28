// SipoDeck ESP32 test firmware'i (Task 010).
//
// Görevi yalnızca tuş olaylarını ve cihaz bilgisini göndermektir; profil veya eylem
// mantığı içermez. Aynı JSON satırları hem USB Serial'e hem de bağlı WebSocket
// istemcilerine gönderilir. Her mesaj tek satırdır ve '\n' ile biter.
//
// Gereken kütüphane: "WebSockets" (Markus Sattler / Links2004) — Arduino Library Manager.

#include <WiFi.h>
#include <WebSocketsServer.h>
#include "config.h"

static const char* FIRMWARE_VERSION = "0.1.0";
static const int PROTOCOL_VERSION = 1;
static const unsigned long DEBOUNCE_MS = 20;

static const int BUTTON_COUNT = sizeof(BUTTON_PINS) / sizeof(BUTTON_PINS[0]);

WebSocketsServer webSocket(WEBSOCKET_PORT);

bool buttonPressed[BUTTON_COUNT];
bool lastReading[BUTTON_COUNT];
unsigned long lastChange[BUTTON_COUNT];

String serialLine;

String helloJson() {
  String json = "{\"version\":";
  json += PROTOCOL_VERSION;
  json += ",\"type\":\"hello\",\"device_id\":\"" DEVICE_ID "\",\"device_name\":\"" DEVICE_NAME "\",\"firmware_version\":\"";
  json += FIRMWARE_VERSION;
  json += "\",\"protocol_version\":";
  json += PROTOCOL_VERSION;
  json += ",\"timestamp\":";
  json += millis();
  json += "}\n";
  return json;
}

String buttonJson(int button, bool pressed) {
  String json = "{\"version\":";
  json += PROTOCOL_VERSION;
  json += ",\"device_id\":\"" DEVICE_ID "\",\"type\":\"button\",\"button\":";
  json += button;
  json += ",\"state\":\"";
  json += pressed ? "pressed" : "released";
  json += "\",\"timestamp\":";
  json += millis();
  json += "}\n";
  return json;
}

bool isHelloRequest(const String& message) {
  return message.indexOf("\"type\":\"hello\"") >= 0;
}

void broadcast(String json) {
  Serial.print(json);
  webSocket.broadcastTXT(json);
}

void onWebSocketEvent(uint8_t client, WStype_t type, uint8_t* payload, size_t length) {
  switch (type) {
    case WStype_CONNECTED: {
      String hello = helloJson();
      webSocket.sendTXT(client, hello);
      break;
    }
    case WStype_TEXT: {
      String message = String((const char*)payload, length);
      if (isHelloRequest(message)) {
        String hello = helloJson();
        webSocket.sendTXT(client, hello);
      }
      break;
    }
    default:
      break;
  }
}

void readSerial() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\n') {
      if (isHelloRequest(serialLine))
        Serial.print(helloJson());
      serialLine = "";
    } else if (serialLine.length() < 256) {
      serialLine += c;
    }
  }
}

void readButtons() {
  unsigned long now = millis();
  for (int i = 0; i < BUTTON_COUNT; i++) {
    bool reading = digitalRead(BUTTON_PINS[i]) == LOW;
    if (reading != lastReading[i]) {
      lastReading[i] = reading;
      lastChange[i] = now;
    }

    if (now - lastChange[i] >= DEBOUNCE_MS && reading != buttonPressed[i]) {
      buttonPressed[i] = reading;
      broadcast(buttonJson(i + 1, reading));
    }
  }
}

void setup() {
  Serial.begin(SERIAL_BAUD);

  for (int i = 0; i < BUTTON_COUNT; i++) {
    pinMode(BUTTON_PINS[i], INPUT_PULLUP);
    buttonPressed[i] = false;
    lastReading[i] = false;
    lastChange[i] = 0;
  }

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  webSocket.begin();
  webSocket.onEvent(onWebSocketEvent);

  // USB Serial ile bağlı masaüstü uygulaması için açılışta kendini tanıtır.
  Serial.print(helloJson());
}

void loop() {
  static bool wifiReported = false;
  if (!wifiReported && WiFi.status() == WL_CONNECTED) {
    // Bilgi satırıdır; JSON olmadığı için masaüstü tarafında güvenle reddedilir.
    Serial.print("# Wi-Fi bagli, IP: ");
    Serial.println(WiFi.localIP());
    wifiReported = true;
  }

  webSocket.loop();
  readSerial();
  readButtons();
}
