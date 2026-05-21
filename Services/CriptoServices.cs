using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using CryptoOrbit.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace CryptoOrbit.Services
{
    public class CriptoService : ICripto
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalServicesOptions _options;

        public CriptoService(IHttpClientFactory httpClientFactory, IOptions<ExternalServicesOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<List<CriptoDto>> GetAllCoins()
        {
            var client = _httpClientFactory.CreateClient("CryptoApi");
            var response = await client.GetAsync($"coins/markets?vs_currency=usd&order=market_cap_desc&per_page=50&page=1&sparkline=false&x-cg-demo-api-key={_options.ApiKeyCoin}");

            var listCoins = await response.Content.ReadFromJsonAsync<List<CriptoDto>>();

            return listCoins ?? new List<CriptoDto>();
        }
    }
}