using CryptoOrbit.Dtos;

namespace CryptoOrbit.Interfaces
{
    public interface ICripto
    {
        Task<List<CriptoDto>> GetAllCoinsAsync(string coinGeckoApiKey, CancellationToken cancellationToken = default);
        Task<List<CriptoDto>> GetAllCoinsWithAnalysisAsync(string coinGeckoApiKey, string groqApiKey, CancellationToken cancellationToken = default);
        Task<CriptoDto> GetCoinByNameAsync(string nameCoin, string coinGeckoApiKey, string groqApiKey, CancellationToken cancellationToken = default);
    }
}
