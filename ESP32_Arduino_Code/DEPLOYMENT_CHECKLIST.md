# ✅ Checklist de Deployment - NecroSENSE v2.2

## 📦 Arquivos Consolidados

### ✅ PRONTO PARA UPLOAD
```
ESP32_Arduino_Code/
├─ ESP32_WiFi_Controller_Complete.ino  ✅ FAZER UPLOAD ESTE
├─ UPLOAD_ESP32.md                     📖 LEIA PRIMEIRO
├─ BME280_TEST.ino                     🧪 Opcional (debug)
└─ SD_CARD_TEST.ino                    🧪 Opcional (debug)
```

### 📖 REFERÊNCIA (No PC - Não Upload)
```
├─ DHT22_BME280_IMPLEMENTACAO.md        📚 Especificações sensores
├─ SD_CARD_INTEGRACAO.md               📚 SD Card endpoints
├─ CONEXAO_SD_CARD_VISUAL.md           📚 Diagrama conexões
└─ README_IMPLEMENTACAO.md             📚 Guia geral
```

---

## 🚀 Sequência Rápida

### FASE 1: Preparação (5 min)
1. [ ] Instalar Arduino IDE
2. [ ] Instalar bibliotecas (veja UPLOAD_ESP32.md)
3. [ ] Conectar ESP32 via USB

### FASE 2: Configuração (2 min)
1. [ ] Abrir `ESP32_WiFi_Controller_Complete.ino`
2. [ ] Alterar `ssid` e `password` (suas credenciais WiFi)
3. [ ] Selecionar Board: ESP32 Dev Module
4. [ ] Selecionar Port: COM correto

### FASE 3: Upload (3 min)
1. [ ] Clique **Verify** (Ctrl+R) - compilar
2. [ ] Clique **Upload** (Ctrl+U) - enviar para ESP32
3. [ ] Aguarde mensagem: ✓ Upload done

### FASE 4: Teste (2 min)
1. [ ] Abrir Serial Monitor (Ctrl+Shift+M)
2. [ ] Verificar inicialização: ✓ BME280, DHT22, UV, SD, WiFi
3. [ ] Anotar IP do ESP32 (ex: 192.168.XXX.XXX)

### FASE 5: Validação (3 min)
1. [ ] Abrir navegador: `http://SEU_IP/status`
2. [ ] Verificar resposta JSON com dados dos sensores
3. [ ] Conectar App MAUI ao IP do ESP32

---

## 🔧 Hardware Requerido

### ✅ Sensores
- [ ] BME280 (I2C: GPIO 21 SDA, GPIO 22 SCL)
- [ ] DHT22 (GPIO 4 + resistor 4.7kΩ pull-up)
- [ ] GUVA-S12SD UV (GPIO 34 analógico)
- [ ] Micro SD Card (SPI: GPIO 5, 18, 19, 23)

### ✅ Controles
- [ ] LED (GPIO 2)
- [ ] 2x Relés (GPIO 26, 27)

### ✅ Poder
- [ ] Fonte 5V para ESP32
- [ ] ⚠️ CRÍTICO: SD Card e sensores em 3.3V (NÃO 5V!)

---

## 📊 Sensores Integrados

| Sensor | Interface | Pino(s) | Status | Endpoints |
|--------|-----------|---------|--------|-----------|
| BME280 | I2C | GPIO 21/22 | ✅ | /temperatura, /umidade, /pressao |
| DHT22 | Digital | GPIO 4 | ✅ | /temperatura/dht22, /umidade/dht22 |
| UV | Analógico | GPIO 34 | ✅ | /uv |
| SD | SPI | GPIO 5,18,19,23 | ✅ | /sd/status, /sd/listar, /sd/deletar |

---

## 🌐 Endpoints Disponíveis

### Status & Controle
- `GET /status` - Status completo do dispositivo
- `GET /sensores` - Array de leituras atuais
- `GET /led/{on,off,toggle}` - Controlar LED
- `GET /rele/{1,2}/{on,off}` - Controlar relés

### Sensores BME280
- `GET /temperatura` - Temperatura
- `GET /umidade` - Umidade
- `GET /pressao` - Pressão

### Sensor DHT22
- `GET /temperatura/dht22` - Temperatura
- `GET /umidade/dht22` - Umidade

### Sensor UV
- `GET /uv` - Índice UV + ADC

### SD Card
- `GET /sd/status` - Conectado/Desconectado
- `GET /sd/listar` - Lista arquivos
- `GET /sd/deletar?arquivo=nome.csv` - Deletar arquivo

---

## ❌ Problemas Comuns & Soluções

### WiFi "Falha na conexão"
```
→ Verifique ssid e password no código
→ Roteador está ligado?
→ ESP32 na mesma rede?
```

### BME280 "Não encontrado"
```
→ Conexões I2C corretas? (GPIO 21, 22)
→ 3.3V conectado? (NÃO 5V!)
→ Teste com BME280_TEST.ino
```

### SD Card "Não conectado"
```
→ CRÍTICO: Voltagem 3.3V (NÃO 5V!)
→ Verifique pinos SPI (GPIO 5,18,19,23)
→ Formate cartão em FAT32
→ Teste com SD_CARD_TEST.ino
```

### "Biblioteca não encontrada"
```
→ Manage Libraries → Instale:
  - ArduinoJson
  - Adafruit_BME280
  - DHT sensor library
→ Reinicie Arduino IDE
```

---

## 📱 Conectar ao App MAUI

Após upload:

1. App MAUI no celular
2. ConfiguraçõPagina → IP do ESP32
3. Inserir: `192.168.XXX.XXX` (ou seu IP)
4. Clique **Conectar**
5. Dashboard mostra dados em tempo real

---

## 📝 Versão & Informações

- **Código**: ESP32_WiFi_Controller_Complete.ino
- **Versão**: 2.2 (com SD Card integrado)
- **Status**: ✅ Pronto para Produção
- **Data**: 2024

---

## 🆘 Precisa de Ajuda?

1. Verifique `UPLOAD_ESP32.md` (guia completo)
2. Verifique `CONEXAO_SD_CARD_VISUAL.md` (conexões)
3. Consulte Serial Monitor (debug)
4. Tente arquivos `*_TEST.ino` para isolar problema

---

**Boa sorte! 🚀**
