using CryptoOrbit.Configurations;
using CryptoOrbit.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CryptoOrbit.Services;

public class NineRouterService : IAiService, IGroqInterfece
{
    private readonly HttpClient _httpClient;
    private readonly NineRouterOptions _options;

    public NineRouterService(HttpClient httpClient, IOptions<ExternalServicesOptions> options = null)
    {
        _httpClient = httpClient;
        _options = options?.Value?.NineRouter ?? new NineRouterOptions();
    }

    public async Task<string> InfoCryptoForCoin(object prompt, string apiKey = null, CancellationToken cancellationToken = default)
    {
        var resolvedKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : _options.ApiKey;

        if (string.IsNullOrWhiteSpace(resolvedKey))
        {
            throw new ArgumentException("A chave de API para o 9router nao foi fornecida (nem via requisicao nem no appsettings.json).", nameof(apiKey));
        }

        var endpoint = !string.IsNullOrWhiteSpace(_options.Endpoint) ? _options.Endpoint : "v1/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(prompt), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resolvedKey.Trim());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Erro ao consultar 9router ({(int)response.StatusCode}): {responseContent}",
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
            throw new InvalidOperationException("O 9router retornou uma resposta vazia.");
        }

        return generatedText
            .Replace("```json", string.Empty)
            .Replace("```", string.Empty)
            .Trim();
    }
}