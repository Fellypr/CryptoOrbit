using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CryptoOrbit.Services
{
    public class CriptoService : ICripto
    {
        private const string MarketQuery = "coins/markets?vs_currency=usd&order=market_cap_desc&per_page=20&page=1&sparkline=false";

        private readonly HttpClient _httpClient;
        private readonly IGroqInterfece _groqService;

        public CriptoService(HttpClient httpClient, IGroqInterfece groqService)
        {
            _httpClient = httpClient;
            _groqService = groqService;
        }

        public async Task<List<CriptoDto>> GetAllCoinsAsync(string coinGeckoApiKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(coinGeckoApiKey))
            {
                throw new ArgumentException("A chave da CoinGecko nao pode ser vazia.", nameof(coinGeckoApiKey));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, MarketQuery);
            request.Headers.TryAddWithoutValidation("x-cg-demo-api-key", coinGeckoApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Erro na CoinGecko ({(int)response.StatusCode}): {responseContent}",
                    null,
                    response.StatusCode);
            }

            var coins = JsonSerializer.Deserialize<List<CriptoDto>>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return coins ?? new List<CriptoDto>();
        }

        public async Task<List<CriptoDto>> GetAllCoinsWithAnalysisAsync(
            string coinGeckoApiKey,
            string groqApiKey,
            CancellationToken cancellationToken = default)
        {
            var coins = await GetAllCoinsAsync(coinGeckoApiKey, cancellationToken);
            var enrichedCoins = new List<CriptoDto>(coins.Count);

            foreach (var coin in coins)
            {
                enrichedCoins.Add(await EnrichCoinAsync(coin, groqApiKey, cancellationToken));

                await Task.Delay(TimeSpan.FromSeconds(5),cancellationToken);
            }

            return enrichedCoins;
        }

        public async Task<CriptoDto> GetCoinByNameAsync(
            string nameCoin,
            string coinGeckoApiKey,
            string groqApiKey,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nameCoin))
            {
                throw new ArgumentException("O nome da moeda nao pode ser vazio.", nameof(nameCoin));
            }

            var coins = await GetAllCoinsAsync(coinGeckoApiKey, cancellationToken);
            var selectedCoin = coins.FirstOrDefault(c =>
                c.Name.Equals(nameCoin, StringComparison.OrdinalIgnoreCase) ||
                c.Symbol.Equals(nameCoin, StringComparison.OrdinalIgnoreCase));

            if (selectedCoin is null)
            {
                return null;
            }

            return await EnrichCoinAsync(selectedCoin, groqApiKey, cancellationToken);
        }

        private async Task<CriptoDto> EnrichCoinAsync(CriptoDto coin, string groqApiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(groqApiKey))
            {
                throw new ArgumentException("A chave da Groq nao pode ser vazia.", nameof(groqApiKey));
            }

            var prompt = BuildPrompt(coin);
            var groqResponse = await _groqService.InfoCryptoForCoin(prompt, groqApiKey, cancellationToken);

            if (!groqResponse.StartsWith("{", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"A Groq nao retornou um JSON valido. Resposta recebida: {groqResponse}");
            }

            var result = JsonSerializer.Deserialize<CriptoDto>(
                groqResponse,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result is null)
            {
                throw new JsonException("Nao foi possivel desserializar a resposta da Groq.");
            }

            coin.Recommendation = result.Recommendation;
            coin.PriceRange = result.PriceRange;
            coin.TotalVolume = result.TotalVolume;

            return coin;
        }

        private static object BuildPrompt(CriptoDto coin)
        {
            return new
            {
                model = "llama-3.3-70b-versatile",
                response_format = new { type = "json_object" },
                temperature = 0.1,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = $@"# INSTRUCOES DO SISTEMA: Analista de Criptoativos (Output JSON)
## Seu papel
Voce e um microsservico de analise financeira estruturada. Sua unica funcao e receber dados de uma criptomoeda, calcular metricas e retornar um objeto JSON valido que corresponda exatamente a estrutura da classe CriptoDto.
## Regras estritas de saida
1. Retorne apenas JSON puro. Nao inclua markdown, explicacoes, saudacoes ou texto fora do JSON.
2. O primeiro caractere da resposta deve ser {{ e o ultimo deve ser }}.
3. O JSON deve conter exatamente as chaves: ""name"", ""symbol"", ""image"", ""current_price"", ""high_24h"", ""low_24h"", ""price_change_percentage_24h"", ""price_range"", ""total_volume"" e ""recommendation"".
## Regras de calculo
1. price_range: calcule a diferenca absoluta entre high_24h e low_24h. Se algum valor for nulo, retorne null.
2. Defina o cenario assim:
- Se price_change_percentage_24h for maior que 1.5%, use ""tendencia de alta"".
- Se price_change_percentage_24h for menor que -1.5%, use ""correcao"".
- Se estiver entre -1.5% e 1.5%, use ""lateralizacao"".
3. recommendation deve ser uma frase completa e detalhada. Nunca retorne apenas ""correcao"", ""tendencia de alta"" ou ""lateralizacao"".
4. Monte o campo recommendation seguindo exatamente este template, substituindo os valores pelos dados recebidos:
""O ativo {coin.Name} ({coin.Symbol}) apresenta um cenario de [cenario] nas ultimas 24 horas, acumulando uma variacao de {coin.PriceChangePercentage24h}%. Com o preco atual cotado em {coin.CurrentPrice}, o ativo registrou uma oscilacao diaria entre a minima de {coin.Low24h} e a maxima de {coin.High24h}, movimentando um volume total de {coin.TotalVolume} no mercado.""
## Dados de entrada
Os dados abaixo representam a criptomoeda a ser analisada."
                    },
                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(coin)
                    }
                }
            };
        }
    }

    public class CriptoCacheBackgroundService : BackgroundService
    {
        private const string CacheKey = "all_cryptos_with_ai";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CriptoCacheBackgroundService> _logger;

        public CriptoCacheBackgroundService(
            IServiceScopeFactory scopeFactory,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<CriptoCacheBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var defaultCoinGeckoApiKey =
                        _configuration["ExternalServices:CoinGeckoApiKey"] ??
                        _configuration["ExternalServices:apiKeyCoin"];

                    var defaultGroqApiKey =
                        _configuration["ExternalServices:GroqApiKey"] ??
                        _configuration["GroqSettings:ApiKey"];

                    if (string.IsNullOrWhiteSpace(defaultCoinGeckoApiKey) || string.IsNullOrWhiteSpace(defaultGroqApiKey))
                    {
                        _logger.LogWarning("Cache em segundo plano ignorado porque as chaves padrao nao foram configuradas.");
                    }
                    else
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var criptoService = scope.ServiceProvider.GetRequiredService<ICripto>();

                        var coins = await criptoService.GetAllCoinsWithAnalysisAsync(
                            defaultCoinGeckoApiKey,
                            defaultGroqApiKey,
                            stoppingToken);

                        _cache.Set(CacheKey, coins, TimeSpan.FromHours(12));
                        _logger.LogInformation("Cache de criptomoedas atualizado com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar o cache de criptomoedas.");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}
