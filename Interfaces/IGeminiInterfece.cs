using CryptoOrbit.Dtos;


namespace CryptoOrbit.Interfaces;

public interface IGeminiInterfece
{
    Task<string> GetInfoCryptoForCoin(string prompt); 
}