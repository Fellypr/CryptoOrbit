namespace CryptoOrbit.Configurations;

public class ExternalServicesOptions
{
    public const string SectionName = "ExternalServices";

    public CoinGeckoOptions CoinGecko { get; set; } = new();
    public NineRouterOptions NineRouter { get; set; } = new();

  
    public string ApiKeyCoin
    {
        get => CoinGecko.ApiKey;
        set => CoinGecko.ApiKey = value;
    }
}

public class CoinGeckoOptions
{
    public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "usd";
    public int DefaultCoinsPerPage { get; set; } = 50;
}

public class NineRouterOptions
{
    public string BaseUrl { get; set; } = "http://localhost:20128/";
    public string Endpoint { get; set; } = "v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "cc/claude-opus-4-6";
    public double Temperature { get; set; } = 0.1;
    public int ThrottlingDelaySeconds { get; set; } = 0;
}

public class CacheOptions
{
    public const string SectionName = "CacheSettings";
    public int AbsoluteExpirationMinutes { get; set; } = 10;
}