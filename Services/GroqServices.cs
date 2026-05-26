using CryptoOrbit.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CryptoOrbit.Services;

public class GroqServices : IGroqInterfece
{
    private readonly HttpClient _httpClient;

    public GroqServices(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> InfoCryptoForCoin(object prompt, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("A chave da Groq nao pode ser vazia.", nameof(apiKey));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(prompt), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Erro na Groq ({(int)response.StatusCode}): {responseContent}",
                null,
                response.StatusCode);
        }

        using var doc = JsonDocument.Parse(responseContent);
        var generatedText = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(generatedText))
        {
            throw new InvalidOperationException("A Groq retornou uma resposta vazia.");
        }

        return generatedText.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
    }
}
