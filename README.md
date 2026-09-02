# SipoDeck

Open-source macro deck platform built with ESP32 and Windows.

SipoDeck is a customizable macro deck system that connects a physical ESP32 device to a Windows desktop application.

## Project Status

🚧 **Early Development**

The project is currently under active development.

## Planned Features

* ESP32 macro deck support
* Windows desktop application
* Wi-Fi and USB communication
* Customizable profiles
* Button combinations
* Sequential action chains
* System and media controls
* Program and URL launching
* Custom notifications
* Plugin system
* Multiple device support
* Future integrations and automation

## Architecture

```text
ESP32 Device
     ↓
Transport
     ↓
Device Events
     ↓
Input Engine
     ↓
Active Profile
     ↓
Action
```

## Repository Structure

```text
SipoDeck/
├── .github/
├── assets/
├── docs/
├── esp32/
├── plugins/
├── src/
├── tasks/
├── CONTRIBUTING.md
├── LICENSE
├── README.md
└── .gitignore
```

## License

MIT License
