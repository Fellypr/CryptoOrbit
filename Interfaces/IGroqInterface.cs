namespace CryptoOrbit.Interfaces
{
    public interface IGroqInterfece
    {
        Task<string> InfoCryptoForCoin(object prompt, string apiKey, CancellationToken cancellationToken = default);
    }
}
