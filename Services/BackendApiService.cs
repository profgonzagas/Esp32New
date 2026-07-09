using ESP32Controller.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ESP32Controller.Services;

/// <summary>
/// Consome a API REST local (ESP32Api backend) para leituras dos sensores.
/// URL padrão: http://<IP_DO_PC>:5000
/// </summary>
public class BackendApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://192.168.XXX.XXX:5000"; // altere para o IP do PC que roda o backend

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsConfigurado => !string.IsNullOrEmpty(_baseUrl);

    public BackendApiService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public void ConfigurarUrl(string ipOuHost, int porta = 5000)
    {
        _baseUrl = $"http://{ipOuHost}:{porta}";
    }

    // -------------------------------------------------------------------------
    // Leitura de dados
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retorna a leitura mais recente do backend.
    /// </summary>
    public async Task<DadosSensores?> ObterUltimaLeituraAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/sensores/latest");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var reading = JsonSerializer.Deserialize<SensorReadingDto>(json, _jsonOptions);
            return reading?.ToDadosSensores();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BackendApi] Erro ObterUltimaLeitura: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retorna as últimas <paramref name="quantidade"/> leituras do backend.
    /// </summary>
    public async Task<List<DadosSensores>> ObterHistoricoAsync(int quantidade = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/sensores/history?count={quantidade}");
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync();
            var readings = JsonSerializer.Deserialize<List<SensorReadingDto>>(json, _jsonOptions);
            return readings?.Select(r => r.ToDadosSensores()).ToList() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BackendApi] Erro ObterHistorico: {ex.Message}");
            return [];
        }
    }

    // -------------------------------------------------------------------------
    // Publicação de dados (quando o app já obteve a leitura do ESP32 via WiFi)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Envia uma leitura obtida do ESP32 para o backend armazenar.
    /// </summary>
    public async Task<bool> PublicarLeituraAsync(DadosSensores dados)
    {
        try
        {
            var dto = SensorReadingDto.FromDadosSensores(dados);
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/sensores", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BackendApi] Erro PublicarLeitura: {ex.Message}");
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // DTO interno (mapeia para DadosSensores do app MAUI)
    // -------------------------------------------------------------------------

    private sealed class SensorReadingDto
    {
        public float Temperatura { get; set; }
        public float Umidade { get; set; }
        public float Pressao { get; set; }
        public float TemperaturaDHT22 { get; set; }
        public float UmidadeDHT22 { get; set; }
        public int NivelUV { get; set; }
        public float IndiceUV { get; set; }
        public int LdrRaw { get; set; }
        public float LdrPercentual { get; set; }
        public string LdrDescricao { get; set; } = string.Empty;
        public int NivelGas { get; set; }
        public bool ChamaDetectada { get; set; }
        public int NivelSom { get; set; }

        public DadosSensores ToDadosSensores() => new()
        {
            Temperatura = Temperatura,
            Umidade = Umidade,
            Pressao = Pressao,
            TemperaturaDHT22 = TemperaturaDHT22,
            UmidadeDHT22 = UmidadeDHT22,
            NivelUV = NivelUV,
            IndiceUV = IndiceUV,
            LdrRaw = LdrRaw,
            LdrPercentual = LdrPercentual,
            LdrDescricao = LdrDescricao,
            NivelGas = NivelGas,
            ChamaDetectada = ChamaDetectada,
            NivelSom = NivelSom,
            UltimaAtualizacao = DateTime.Now
        };

        public static SensorReadingDto FromDadosSensores(DadosSensores d) => new()
        {
            Temperatura = d.Temperatura,
            Umidade = d.Umidade,
            Pressao = d.Pressao,
            TemperaturaDHT22 = d.TemperaturaDHT22,
            UmidadeDHT22 = d.UmidadeDHT22,
            NivelUV = d.NivelUV,
            IndiceUV = d.IndiceUV,
            LdrRaw = d.LdrRaw,
            LdrPercentual = d.LdrPercentual,
            LdrDescricao = d.LdrDescricao,
            NivelGas = d.NivelGas,
            ChamaDetectada = d.ChamaDetectada,
            NivelSom = d.NivelSom
        };
    }
}
