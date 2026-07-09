namespace ESP32Api.Models;

public class SensorReading
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // BME280
    public float Temperatura { get; set; }
    public float Umidade { get; set; }
    public float Pressao { get; set; }

    // DHT22
    public float TemperaturaDHT22 { get; set; }
    public float UmidadeDHT22 { get; set; }

    // UV
    public int NivelUV { get; set; }
    public float IndiceUV { get; set; }

    // LDR
    public int LdrRaw { get; set; }
    public float LdrPercentual { get; set; }
    public string LdrDescricao { get; set; } = string.Empty;

    // Outros
    public int NivelGas { get; set; }
    public bool ChamaDetectada { get; set; }
    public int NivelSom { get; set; }
}
