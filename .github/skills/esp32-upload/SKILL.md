---
name: esp32-upload
description: "Upload (flash) Arduino/PlatformIO code to ESP32 microcontroller. Use when: deploying firmware, flashing ESP32, uploading .ino code, programming ESP32, sending code to board."
argument-hint: "Optional: COM port (default COM5) or 'monitor' to also open serial monitor after upload"
---

# ESP32 Upload

Compile and upload firmware to the ESP32 board using PlatformIO CLI.

## When to Use

- User asks to upload/flash/deploy code to the ESP32
- User wants to program the ESP32 board
- User says "faz upload", "envia pro ESP32", "grava no ESP32", "flasha"
- After making changes to `.ino` files and wanting to test on hardware

## Prerequisites

- ESP32 connected via USB
- PlatformIO CLI installed (`pio` command available)
- Project has a `platformio.ini` file

## Procedure

1. **Navigate to the PlatformIO project directory:**
   ```
   cd ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete
   ```

2. **Verify ESP32 is connected** (optional but recommended on first use):
   ```
   pio device list
   ```
   The ESP32 should appear on **COM5** (USB VID:PID=0403:6001). If on a different port, update `platformio.ini` accordingly.

3. **Compile and upload:**
   ```
   pio run --target upload
   ```
   This compiles the code and flashes it to the ESP32 in one step.

4. **Verify success:** Look for `[SUCCESS]` in the output. The ESP32 resets automatically after flashing.

5. **Open serial monitor** (if requested or needed for debugging):
   ```
   pio device monitor
   ```
   Press `Ctrl+C` to exit the monitor.

## Project Layout

- **Code:** `ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete/src/main.cpp` (compilado pelo PlatformIO)
- **Config:** `ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete/platformio.ini`
- **Board:** `esp32dev` (ESP32-D0WD-V3)
- **Upload port:** COM5 (921600 baud)
- **Monitor port:** COM5 (115200 baud)

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Port not found | Run `pio device list` to check the correct COM port. Update `upload_port` in `platformio.ini`. |
| Permission denied | Close Arduino IDE or any other serial monitor that may be using the port. |
| Upload fails with timeout | Hold the **BOOT** button on the ESP32 while uploading, release after "Connecting..." appears. |
| Compilation error | Fix the code first. Run `pio run` (without `--target upload`) to compile only. |

---

## Architecture Notes (NecroSense ESP32 Project)

### Critical: Firmware fica em `src/main.cpp`
- PlatformIO **sempre compila `src/main.cpp`** — o arquivo `.ino` raiz é **ignorado** quando `src/main.cpp` existe.
- **Todas as alterações de firmware devem ser feitas em `src/main.cpp`**.
- O antigo `ESP32_WiFi_Controller.ino` na raiz foi **removido** (era só uma cópia desatualizada para Arduino IDE).

### Credentials & Secrets
- `src/secrets.h`: WiFi SSID/pass, MQTT server/port/user/pass — gitignored, keep out of source control.
- **MQTT** (ESP32): `<seu-cluster>.s1.eu.hivemq.cloud`, port `8883` (TLS), using `PubSubClient`.
- **MQTT** (Dashboard JS): `wss://<seu-cluster>.s1.eu.hivemq.cloud:8884/mqtt`, using `mqtt.js`.
- **Firebase**: `https://<seu-projeto>-default-rtdb.firebaseio.com/leituras.json`, rules `".write": "auth != null"` (escrita autenticada via `?auth=`), POST JSON.

### TLS / Heap Constraint
- `WiFiClientSecure` consumes ~40KB heap per instance.
- Running two simultaneous TLS sessions (MQTT + Firebase) causes silent Firebase failures.
- **Fix**: disconnect MQTT before opening Firebase TLS, then set `ultimaTentativaMQTT = 0` so MQTT reconnects immediately after.

### Key Timers in `main.cpp`
| Constant | Value | Purpose |
|---|---|---|
| `INTERVALO_RETRY_MQTT` | 60000 ms | MQTT reconnect interval (60s prevents loop starvation) |
| `FIREBASE_INTERVAL` | 30000 ms | Firebase save interval |
| Publish interval | 30000 ms | MQTT sensor data publish |

### `loop()` Firebase Block
```cpp
if (WiFi.status() == WL_CONNECTED) {
    unsigned long agoraFb = millis();
    if (agoraFb - ultimaSalvagemFirebase >= FIREBASE_INTERVAL) {
        ultimaSalvagemFirebase = agoraFb;
        salvarNoFirebase();
    }
}
```

### `salvarNoFirebase()` Pattern
```cpp
// 1. Disconnect MQTT to free TLS slot
if (mqttClient.connected()) {
    mqttClient.disconnect();
    mqttConectado = false;
    delay(200);
}
ultimaTentativaMQTT = 0; // reconnect immediately after

// 2. Fresh local TLS client (one session at a time)
WiFiClientSecure fbClient;
fbClient.setInsecure();
fbClient.setTimeout(10);

HTTPClient https;
https.setTimeout(10000);
https.setFollowRedirects(HTTPC_STRICT_FOLLOW_REDIRECTS);
if (https.begin(fbClient, FIREBASE_URL)) {
    https.addHeader("Content-Type", "application/json");
    int code = https.POST(body);
    https.end();
}
fbClient.stop();
```

### Dashboard Fixes
- `dashboard/google.html` and `dashboard/index.html`: changed `sessionStorage` → `localStorage` for `mqtt_u`/`mqtt_p` so MQTT credentials survive browser restarts.

### Partition
- Uses `huge_app.csv` partition to fit firmware (Flash ~59%, RAM ~19% at last build).

### Serial Monitor
```
pio device monitor -p COM5 -b 115200
```
After ~30s: look for `[Firebase] ✓ Salvo #1`  
After ~60s: look for `[MQTT] ✓ Conectado!`

---

## Hardware — Placa ESP32

**Modelo:** ESP32-DevKitC (38 pinos, módulo ESP32-WROOM-32)

### Pinout (labels visíveis na placa, esquerda → direita)

| Label placa | GPIO | Observação |
|---|---|---|
| VIN | — | Alimentação 5V via USB |
| GND | GND | |
| D13 | 13 | |
| D12 | 12 | |
| D14 | 14 | **SD_CS** (TF_CS no módulo do display MSP1803) |
| D27 | 27 | Relé 2 |
| D26 | 26 | Relé 1 |
| D25 | 25 | |
| D33 | 33 | UV_DO_PIN (LM393 saída digital) |
| D32 | 32 | UV_AO_PIN (LM393 saída analógica) |
| D35 | 35 | Somente entrada (sem pull-up interno) |
| D34 | 34 | UV_PIN (GUVA-S12SD direto) — somente entrada |
| UN | — | |
| UP | — | |
| EN | EN | Reset |
| 3V3 | 3.3V | Alimentação sensores |
| GND | GND | |
| D15 | 15 | TFT_LED (backlight display) |
| D2 | 2 | LED_PIN (LED built-in) |
| D4 | 4 | |
| RX2 | 16 | **TFT_RST** (RESET do display ST7735S) |
| TX2 | 17 | **TFT_DC** (DC/A0 do display ST7735S) |
| D21 | 21 | I2C SDA (BME280) |
| RX0 | 3 | UART0 RX (Serial monitor) |
| TX0 | 1 | UART0 TX (Serial monitor) |
| D22 | 22 | I2C SCL (BME280) |
| D23 | 23 | SPI MOSI → TFT SDA |
| D18 | 18 | SPI CLK  → TFT SCK |
| D19 | 19 | SPI MISO (livre, ex-SD card) |
| D5 | 5 | **TFT_CS** (Chip Select display) |

> ⚠️ GPIO 34, 35 são **somente entrada** — sem pull-up interno e sem saída digital.
> ⚠️ RX2 (GPIO 16) e TX2 (GPIO 17) são UART2 — livres pois o projeto não usa UART2.

---

## Sensores Conectados

### GUVA-S12SD + LM393 (UV)
| Pino sensor | GPIO | Label placa |
|---|---|---|
| OUT (direto) | 34 | D34 |
| AO (via LM393) | 32 | D32 |
| DO (via LM393) | 33 | D33 |
| VCC | 3.3V | 3V3 |
| GND | GND | GND |

### Display TFT 1.8" ST7735S (MSP1803) — com leitor SD embutido
| Pino display | GPIO | Label placa |
|---|---|---|
| **TFT_CS** | 5 | D5 |
| **TF_CS** (SD) | 14 | D14 |
| RESET | 16 | RX2 |
| DC/A0 | 17 | TX2 |
| SDA (MOSI) | 23 | D23 (compartilhado TFT+SD) |
| SCK | 18 | D18 (compartilhado TFT+SD) |
| MISO | 19 | D19 (só SD usa) |
| LED/BL | 15 | D15 |
| VCC | 3.3V (ou 5V) | 3V3 / VIN |
| GND | GND | GND |

**Bibliotecas (PlatformIO `lib_deps`):**  
`adafruit/Adafruit GFX Library` + `adafruit/Adafruit ST7735 and ST7789 Library`  
*(NÃO usar LCDWIKI — quebra o build no ESP32: macros `CD_DATA`/`CS_IDLE` indefinidas.)*

### Relés
| Relé | GPIO | Label placa |
|---|---|---|
| Relé 1 | 26 | D26 |
| Relé 2 | 27 | D27 |
