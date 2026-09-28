// SipoDeck ESP32 yapılandırması.
// Bu dosyayı aynı klasöre "config.h" adıyla kopyalayıp kendi değerlerini gir.
// config.h git'e eklenmez; gerçek Wi-Fi bilgilerini bu örnek dosyaya yazma.

#pragma once

// Wi-Fi
#define WIFI_SSID     "YOUR_WIFI_SSID"
#define WIFI_PASSWORD "YOUR_WIFI_PASSWORD"

// Masaüstü uygulamasının bağlanacağı WebSocket portu (ayarlardaki Port ile aynı olmalı).
#define WEBSOCKET_PORT 81

// USB Serial hızı (ayarlardaki BaudRate ile aynı olmalı).
#define SERIAL_BAUD 115200

// Cihaz kimliği ve adı.
#define DEVICE_ID   "sipodeck-01"
#define DEVICE_NAME "SipoDeck"

// Tuş pinleri. Sıra tuş numarasını belirler: ilk pin = tuş 1, ikinci pin = tuş 2 ...
// Tuşlar pin ile GND arasına bağlanır (dahili pull-up kullanılır).
static const int BUTTON_PINS[] = { 13, 12, 14, 27 };
