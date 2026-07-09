namespace ESP32Controller.Models;

public class DispositivoESP32
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = "NecroSENSE ESP32";
    public string EnderecoIP { get; set; } = "192.168.XXX.XXX";
    public int Porta { get; set; } = 80;
    public string EnderecoMAC { get; set; } = "00:4B:12:XX:XX:XX";
    public bool ConectadoWiFi { get; set; }
    public bool ConectadoBLE { get; set; }
    public DateTime UltimaConexao { get; set; }
    
    public string UrlBase => $"http://{EnderecoIP}:{Porta}";
}
