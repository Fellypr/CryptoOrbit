namespace CryptoOrbit.Interfaces;

public interface IAiService
{
    Task<string> InfoCryptoForCoin(object prompt, string apiKey = null, CancellationToken cancellationToken = default);
}