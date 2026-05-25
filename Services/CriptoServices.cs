using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using CryptoOrbit.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoOrbit.Services
{
    public class CriptoService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalServicesOptions _options;
        private readonly IServiceProvider _servicesProvider;
        private readonly IMemoryCache _cache;

        public CriptoService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalServicesOptions> options,
            IServiceProvider servicesProvider,
            IMemoryCache cache
        )
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _servicesProvider = servicesProvider;
            _cache = cache;
        }

        public async Task<List<CriptoDto>> GetAllCoins()
        {
            var client = _httpClientFactory.CreateClient("CryptoApi");
            var response = await client.GetAsync($"coins/markets?vs_currency=usd&order=market_cap_desc&per_page=3&page=1&sparkline=false&x-cg-demo-api-key={_options.ApiKeyCoin}");

            var listCoins = await response.Content.ReadFromJsonAsync<List<CriptoDto>>();

            return listCoins ?? new List<CriptoDto>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                List<CriptoDto> listCoins = new List<CriptoDto>();
                List<CriptoDto> moedasComIaPronta = new List<CriptoDto>();
                try
                {
                    listCoins = await GetAllCoins();

                    using (var scope = _servicesProvider.CreateScope())
                    {
                        var geminiServices = scope.ServiceProvider.GetRequiredService<IGeminiInterfece>();
                        foreach (var moeda in listCoins)
                        {
                            if (stoppingToken.IsCancellationRequested) break;
                            try
                            {
                                CriptoDto moedaAtualizada = await GetCryptoById(moeda,geminiServices ,stoppingToken);

                                if (moedaAtualizada != null)
                                {
                                    moedasComIaPronta.Add(moedaAtualizada);
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error ao procesar o nome" + ex);

                            }



                        }
                    }


                    if (moedasComIaPronta.Any())
                    {
                        _cache.Set("all_cryptos_with_ai", moedasComIaPronta, TimeSpan.FromDays(7));
                    }
                    ;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error ao rodar o codigo m segundo plano mais detalhe:{ex}");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            }

        }
        public async Task<CriptoDto> GetCryptoById(CriptoDto moeda, IGeminiInterfece geminiServices,CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"# INSTRUÇÕES DO SISTEMA: Analista de Criptoativos (Output JSON)

## Seu Papel:
Você é um microsserviço de análise financeira estruturada. Sua única função é receber dados de uma criptomoeda, calcular métricas e retornar um objeto JSON válido que corresponda exatamente à estrutura da classe `CriptoDto`.

## Restrições Estritas de Saída (Output):
1. **APENAS JSON PURO:** Retorne única e exclusivamente o objeto JSON. Não inclua blocos de código (como ```json ... 
```), explicações, saudações ou preâmbulos. O primeiro caractere da resposta deve ser {{ e o último deve ser }}.
2. **CAMPOS OBRIGATÓRIOS NO JSON:** O JSON de saída deve conter exatamente as chaves: ""name"", ""symbol"", ""image"", ""current_price"", ""high_24h"", ""low_24h"", ""price_change_percentage_24h"", ""price_range"", ""total_volume"" e ""recommendation"".

## Regras de Cálculo e Lógica:
1. **price_range:** Calcule a diferença absoluta entre {moeda.High24h} e {moeda.Low24h} (high_24h - low_24h). Se algum deles for nulo, retorne null.
2. **Definição de Cenário (Para a Recomendação):**
   - Se price_change_percentage_24h for maior que 1.5%: use o termo ""tendência de alta"".
   - Se price_change_percentage_24h for menor que -1.5%: use o termo ""correção"".
   - Se estiver entre -1.5% e 1.5%: use o termo ""lateralização"".

3. **recommendation (O Texto Descritivo):**
   Monte estritamente o texto deste campo seguindo este template exato (substituindo os valores entre colchetes):
   ""O ativo {moeda.Name} ({moeda.Symbol}) apresenta um cenário de [cenário] nas últimas 24 horas, acumulando uma variação de {moeda.PriceChangePercentage24h}%. Com o preço atual cotado em {moeda.CurrentPrice}, o ativo registrou uma oscilação diária entre a mínima de {moeda.Low24h} e a máxima de {moeda.High24h}, movimentando um volume total de {moeda.TotalVolume} no mercado.""

## Dados de Entrada:
Os dados serão fornecidos pelo sistema no formato de propriedades brutas do ativo.""";


                var geminiResponse = await geminiServices.GetInfoCryptoForCoin(prompt);
                if (geminiResponse == null)
                {
                    throw new Exception("Erro ao obter dados da moeda");
                }
                var result = JsonSerializer.Deserialize<CriptoDto>(geminiResponse);

                if (result != null)
                {
                    moeda.Recommendation = result.Recommendation;
                    moeda.PriceRange = result.PriceRange;
                    moeda.TotalVolume = result.TotalVolume;
                }
                return moeda;
            }
            catch (HttpOperationException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Console.WriteLine($"[erro 503 gemini] olha o error de serviço temporariamente indisponivel{ex}");
                throw new("O serviço de IA está temporariamente indisponível. Tente novamente em instantes.");

            }
            catch (Exception ex)
            {
                throw new($"Error geral {ex}");
            }


        }

    }
}