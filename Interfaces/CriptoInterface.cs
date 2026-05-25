using CryptoOrbit.Dtos;
using Microsoft.AspNetCore.Mvc;


namespace CryptoOrbit.Interfaces
{
    public interface ICripto
    {
        Task<List<CriptoDto>> GetAllCoins();
        Task<CriptoDto>GetCryptoById(CriptoDto moeda,CancellationToken cancellationToken);
    }
}