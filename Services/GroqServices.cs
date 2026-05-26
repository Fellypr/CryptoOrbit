using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace CryptoOrbit.Services;

public class GroqServices : IGroqInterfece
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public GroqServices(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GroqSettings:ApiKey"] ?? throw new ArgumentNullException("Chave api não configurada");

    }
    public async Task<string> InfoCryptoForCoin(Object prompt)
    {
        try
        {
            var requestUri = "https://api.groq.com/openai/v1/chat/completions";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(prompt), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var jsonTextoDaIa = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(jsonTextoDaIa))
            {
                throw new InvalidOperationException("A Groq retornou uma resposta vazia.");
            }

            string clearText = jsonTextoDaIa.Replace("```json", "").Replace("```", "").Trim();

            return clearText;

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao usar a IA na Groq.", ex);
        }
    }
}
