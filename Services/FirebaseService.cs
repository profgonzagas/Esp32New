using ESP32Controller.Models;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ESP32Controller.Services;

/// <summary>
/// Consome o Firebase Realtime Database do projeto NecroSENSE.
/// Não requer NuGet extra — usa apenas HttpClient com a REST API do Firebase.
///
/// URL base: https://SEU-PROJETO-default-rtdb.firebaseio.com
///
/// Campos do JSON enviados pelo ESP32:
///   timestamp, temp_bme, umid_bme, pressao,
///   temp_dht, umid_dht, indice_uv, ldr_nivel,
///   uptime, sd_card, wifi_rssi, free_heap, mqtt_ok
/// </summary>
public class FirebaseService
{
    private const string DbUrl = "https://SEU-PROJETO-default-rtdb.firebaseio.com";
    private const string LeiturasPath = "/leituras";

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public FirebaseService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // -------------------------------------------------------------------------
    // Leitura pontual
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retorna a leitura mais recente salva no Firebase.
    /// Usa <c>limitToLast=1</c> — as push keys do Firebase são ordenadas por tempo.
    /// </summary>
    public async Task<DadosSensores?> ObterUltimaLeituraAsync()
    {
        try
        {
            // limitToLast=1 retorna o entry mais recente (push key é time-ordered)
            var url = $"{DbUrl}{LeiturasPath}.json?limitToLast=1";
            var json = await _httpClient.GetStringAsync(url);

            var dict = JsonSerializer.Deserialize<Dictionary<string, FirebaseEntry>>(json, _jsonOpts);
            var entry = dict?.Values.FirstOrDefault();
            return entry?.ToDadosSensores();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] Erro ObterUltima: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retorna as últimas <paramref name="quantidade"/> leituras do Firebase (ordenadas por tempo).
    /// </summary>
    public async Task<List<DadosSensores>> ObterHistoricoAsync(int quantidade = 50)
    {
        try
        {
            var url = $"{DbUrl}{LeiturasPath}.json?limitToLast={Math.Clamp(quantidade, 1, 500)}";
            var json = await _httpClient.GetStringAsync(url);

            var dict = JsonSerializer.Deserialize<Dictionary<string, FirebaseEntry>>(json, _jsonOpts);
            if (dict is null) return [];

            // Ordenar pelas push keys (já são time-ordered, mas garantimos a ordem)
            return dict
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value.ToDadosSensores())
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] Erro ObterHistorico: {ex.Message}");
            return [];
        }
    }

    // -------------------------------------------------------------------------
    // Streaming em tempo real (Server-Sent Events)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Escuta atualizações em tempo real do Firebase via SSE.
    /// Produz <see cref="DadosSensores"/> a cada nova leitura do ESP32.
    /// Cancele via <paramref name="cancellationToken"/>.
    /// </summary>
    /// <example>
    /// await foreach (var leitura in firebaseService.EscutarEmTempoRealAsync(cts.Token))
    /// {
    ///     MinhaPropriedade = leitura;
    /// }
    /// </example>
    public async IAsyncEnumerable<DadosSensores> EscutarEmTempoRealAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Firebase SSE: GET .json com Accept: text/event-stream
        var url = $"{DbUrl}{LeiturasPath}.json";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] Erro SSE connect: {ex.Message}");
            response?.Dispose();
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        string? data = null;

        while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] Erro SSE read: {ex.Message}");
                break;
            }

            if (line is null) continue;

            if (line.StartsWith("data:"))
            {
                data = line["data:".Length..].Trim();
            }
            else if (line.Length == 0 && data is not null)
            {
                // Linha em branco = fim do evento SSE
                var leitura = ParseSseData(data);
                if (leitura is not null)
                    yield return leitura;

                data = null;
            }
        }

        response.Dispose();
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    private static DadosSensores? ParseSseData(string data)
    {
        try
        {
            // O payload SSE do Firebase é: {"path":"/...","data":{<entry>}}
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataEl)) return null;
            if (dataEl.ValueKind == JsonValueKind.Null) return null;

            // Se "data" for um objeto com uma única push key (novo registro)
            if (dataEl.ValueKind == JsonValueKind.Object)
            {
                // Pode ser o objeto completo do nó /leituras ou uma entry única
                // Tentar deserializar como FirebaseEntry primeiro
                var entry = JsonSerializer.Deserialize<FirebaseEntry>(dataEl.GetRawText(), _jsonOpts);
                if (entry?.TempBme != 0 || entry?.TempDht != 0)
                    return entry?.ToDadosSensores();

                // Caso contrário, pegar o último valor do dict
                var dict = JsonSerializer.Deserialize<Dictionary<string, FirebaseEntry>>(
                    dataEl.GetRawText(), _jsonOpts);
                return dict?.OrderBy(kv => kv.Key).LastOrDefault().Value?.ToDadosSensores();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // DTO que espelha o JSON enviado pelo ESP32
    // -------------------------------------------------------------------------

    private sealed class FirebaseEntry
    {
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("temp_bme")]
        public float TempBme { get; set; }

        [JsonPropertyName("umid_bme")]
        public float UmidBme { get; set; }

        [JsonPropertyName("pressao")]
        public float Pressao { get; set; }

        [JsonPropertyName("temp_dht")]
        public float TempDht { get; set; }

        [JsonPropertyName("umid_dht")]
        public float UmidDht { get; set; }

        [JsonPropertyName("indice_uv")]
        public float IndiceUv { get; set; }

        [JsonPropertyName("ldr_nivel")]
        public int LdrNivel { get; set; }

        [JsonPropertyName("uptime")]
        public long Uptime { get; set; }

        [JsonPropertyName("wifi_rssi")]
        public int WifiRssi { get; set; }

        public DadosSensores ToDadosSensores()
        {
            DateTime ts = DateTime.Now;
            if (Timestamp is not null)
                DateTime.TryParse(Timestamp, out ts);

            return new DadosSensores
            {
                Temperatura         = TempBme,
                Umidade             = UmidBme,
                Pressao             = Pressao,
                TemperaturaDHT22    = TempDht,
                UmidadeDHT22        = UmidDht,
                IndiceUV            = IndiceUv,
                NivelUV             = LdrNivel,     // ldr_nivel ~ nivelUV (raw ADC)
                UltimaAtualizacao   = ts
            };
        }
    }
}
