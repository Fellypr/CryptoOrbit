using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CryptoOrbit.Services
{
    public class CriptoService : ICripto
    {
        private const string MarketQuery = "coins/markets?vs_currency=usd&order=market_cap_desc&per_page=50&page=1&sparkline=false";

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

                await Task.Delay(TimeSpan.FromSeconds(7),cancellationToken);
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
4. Monte o campo recommendation seguindo exatamente a lógica abaixo, substituindo os valores entre chaves  pelos dados recebidos. Você deve escolher apenas uma das duas ramificações (COMPRAR ou AGUARDAR) dependendo da sua análise de mercado:

O ativo {coin.Name} ({coin.Symbol}) apresenta um cenário de [cenário] nas últimas 24 horas, acumulando uma variação de {coin.PriceChangePercentage24h}%. Com o preço atual cotado em {coin.CurrentPrice}, o ativo registrou uma oscilação diária entre a mínima de {coin.Low24h} e a máxima de {coin.High24h}, movimentando um volume total de {coin.TotalVolume} no mercado.

[SE A SUA ANÁLISE FOR DE COMPRA, ADICIONE ESTE PARÁGRAFO]:
Com base nos indicadores técnicos e no fluxo de volume, nossa IA recomenda a COMPRA do ativo. Para gerenciamento de risco, estimamos uma alocação sugerida de [insira aqui uma % realista entre 1% e 5%] do seu capital disponível, o que equivaleria a um aporte de aproximadamente [calcule o valor com base na % sugerida e no saldo do usuário] na cotação atual. Essa estratégia visa surfar a tendência sem expor excessivamente a sua carteira.

[SE A SUA ANÁLISE FOR DE NÃO COMPRAR, ADICIONE ESTE PARÁGRAFO]:
No momento, nossa IA recomenda AGUARDAR e não realizar compras. O ativo enfrenta uma desvalorização/pressão de venda que pode se estender, acumulando uma perda de {coin.PriceChangePercentage24h}% nas últimas 24 horas. Entrar no mercado agora significa correr o risco de 'pegar uma faca caindo'. Sugerimos esperar o preço testar a região de suporte em {coin.Low24h} antes de uma nova avaliação.""
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


}
