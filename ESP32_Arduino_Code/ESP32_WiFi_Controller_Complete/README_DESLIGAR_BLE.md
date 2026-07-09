# Plano B — Desligar o BLE para estabilizar WiFi + Firebase

> Procedimento de contingência. **NÃO precisa aplicar agora** — o firmware atual
> mantém o BLE ligado e está funcionando (WiFi conecta e Firebase salva).
> Use isto **apenas se** voltarem os sintomas de falta de memória/instabilidade.

---

## Quando usar

Aplique este plano se, com o BLE ligado, começarem a aparecer no log serial (115200 baud, COM5):

- `ssl_client.cpp _handle_error(): (-16256) PK - Memory allocation failed`
- `[Firebase] ERRO conexao: connection refused` recorrente (Firebase para de salvar)
- `[WDT] HEAP CRITICO: ... reiniciando ESP32!` (reinícios por heap baixo)
- O app desconectando do WiFi de forma frequente / `/status` dando timeout
- Heap livre antes do save do Firebase consistentemente abaixo de ~55 KB

A causa é sempre a mesma: **BLE + WiFi + TLS (Firebase) não cabem juntos na RAM**
deste ESP32. O BLE sozinho consome ~50 KB de heap e ainda divide o mesmo rádio
2.4 GHz com o WiFi (o que provoca quedas de conexão).

## O que esse plano resolve

- **Libera ~50 KB de heap** → o handshake TLS do Firebase passa a ter folga e para de falhar.
- **Acaba com a disputa de rádio WiFi/BLE** → bem menos quedas de WiFi.
- Firmware mais simples e previsível.

## O que se perde

- A tela **BLEControlPage** do app para de funcionar (o ESP32 não anuncia mais BLE).
- **Não há perda de funcionalidade real:** todo o controle continua disponível por
  WiFi/HTTP — os mesmos endpoints (`/led/on`, `/led/off`, `/rele/1/on`, `/sensores`,
  `/status`, etc.). Use a tela **WiFiControlPage** do app.

---

## Procedimento (mínimo — 3 passos)

Tudo em [`src/main.cpp`](src/main.cpp).

### Passo 1 — Liberar a memória do Bluetooth no início do `setup()`

Adicione o include junto aos demais (perto do topo do arquivo):

```cpp
#include "esp_bt.h"   // para esp_bt_controller_mem_release()
```

E como **primeira linha dentro de `setup()`** (logo após `Serial.begin(115200);`),
devolva ao heap toda a RAM reservada para Bluetooth:

```cpp
void setup() {
  Serial.begin(115200);
  delay(100);

  // PLANO B: BLE desligado — devolve ~50KB de RAM do Bluetooth ao heap.
  esp_bt_controller_mem_release(ESP_BT_MODE_BTDM);
  ...
```

### Passo 2 — Não inicializar o BLE (linha ~1992 do `setup()`)

Comente a chamada:

```cpp
  // Inicializar BLE (antes do WiFi)
  // inicializarBLE();   // PLANO B: BLE desligado
```

### Passo 3 — Não gerenciar reconexão BLE no `loop()` (linha ~2082)

Comente a chamada:

```cpp
  // Gerenciar BLE (reconexão)
  // gerenciarBLE();      // PLANO B: BLE desligado
```

> Só isso já basta. As funções `inicializarBLE()`, `gerenciarBLE()`,
> `processarComandoBLE()`, os callbacks e os objetos BLE podem continuar no
> arquivo sem problema — como nunca são chamados, **não consomem RAM**.

---

## Compilar e gravar

Pela raiz do firmware (`ESP32_WiFi_Controller_Complete`):

```bash
pio run -t upload --upload-port COM5
```

(Ajuste `COM5` para a porta do seu ESP32 — veja com `pio device list`.)

## Como confirmar que funcionou

Abra o monitor serial:

```bash
pio device monitor --port COM5 --baud 115200
```

Você deve observar:

- **Não** aparece mais o bloco `=== INICIALIZANDO BLE ===` no boot.
- O heap livre antes do save do Firebase sobe bastante (de ~61 KB para ~110 KB+):
  `[Firebase] Tentando salvar... (heap: 11xxxx)`.
- O Firebase salva de forma consistente: `[Firebase] OK #N (HTTP 200, ...)`.
- WiFi estável; o app conecta e atualiza sem timeouts.

---

## (Opcional) Limpeza completa

Se quiser remover o BLE de vez (reduz o tamanho do firmware), apague também:

- Includes: `BLEDevice.h`, `BLEServer.h`, `BLEUtils.h`, `BLE2902.h`
- Defines: `BLE_DEVICE_NAME`, `SERVICE_UUID`, `CHARACTERISTIC_TX`, `CHARACTERISTIC_RX`
- Objetos globais: `pServer`, `pTxCharacteristic`, `pRxCharacteristic`,
  `bleDeviceConnected`, `bleOldDeviceConnected`
- Classes `MyServerCallbacks` e `MyBLECallbacks`
- Funções `inicializarBLE()`, `processarComandoBLE()`, `enviarRespostaBLE()`,
  `gerenciarBLE()` e suas forward-declarations

Não é necessário para o ganho de memória — o ganho vem de **não inicializar** o BLE
(Passos 1–3).

---

## Como reverter (religar o BLE)

Desfaça os 3 passos: remova o `esp_bt_controller_mem_release(...)` e descomente
`inicializarBLE();` e `gerenciarBLE();`. Recompile e grave.

---

## Histórico / contexto

O firmware atual roda **BLE + WiFi + Firebase juntos**. Para isso funcionar, o
envio ao Firebase é feito *inline* no `loop()` (bloqueia ~2-5 s a cada 30 s durante
o handshake TLS) e a reconexão de WiFi foi tornada robusta (nunca desiste; escala
`reconnect()` → ciclo completo `disconnect()/begin()` → `ESP.restart()` como último
recurso, em `verificarConexaoWiFi()`).

A tentativa de mover o Firebase para uma task dedicada no Core 0 (para não bloquear
o HTTP) custou ~8 KB de stack e fez o heap estourar no TLS
(`PK - Memory allocation failed`). Por isso foi revertida. A solução definitiva
para ter Firebase não-bloqueante **e** estável é este Plano B: desligar o BLE.
