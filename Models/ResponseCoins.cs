using System.Text.Json.Serialization;

namespace CryptoOrbit.Models
{
    public class ResponseCoins
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("image")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("current_price")]
        public decimal CurrentPrice { get; set; }

        [JsonPropertyName("high_24h")]
        public decimal High24h { get; set; }

        [JsonPropertyName("low_24h")]
        public decimal Low24h { get; set; }

        [JsonPropertyName("price_change_percentage_24h")]
        public decimal PriceChangePercentage24h { get; set; }

        [JsonPropertyName("total_volume")]
        public decimal TotalVolume { get; set; }
    }
}