using CryptoOrbit.Services;

namespace CryptoOrbit.Interfaces
{
    public interface IGroqInterfece
    {
        Task<string>InfoCryptoForCoin(Object prompt);
        
    }
}