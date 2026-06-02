using CryptoOrbit.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoOrbit.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CriptoController : ControllerBase
    {
        private readonly ICripto _criptoService;
        private readonly IMemoryCache _cache;

        public CriptoController(ICripto criptoService, IMemoryCache cache)
        {
            _criptoService = criptoService;
            _cache = cache;
        }

        [HttpGet("get-all-coins")]
        public async Task<IActionResult> GetAllCoins(
            [FromHeader(Name = "x-cg-demo-api-key")] string coinGeckoApiKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(coinGeckoApiKey))
            {
                return BadRequest("O header x-cg-demo-api-key e obrigatorio.");
            }

            try
            {
                var coins = await _criptoService.GetAllCoinsAsync(
                    coinGeckoApiKey,
                    cancellationToken);

                return Ok(coins);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Erro ao consultar os provedores externos: {ex.Message}");
            }
        }

        [HttpGet("{nameCoin}")]
        public async Task<IActionResult> GetInfoCoin(
            string nameCoin,
            [FromHeader(Name = "x-cg-demo-api-key")] string coinGeckoApiKey,
            [FromHeader(Name = "X-Groq-Key")] string groqApiKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nameCoin))
            {
                return BadRequest("O nome da moeda e obrigatorio.");
            }

            if (string.IsNullOrWhiteSpace(coinGeckoApiKey))
            {
                return BadRequest("O header x-cg-demo-api-key e obrigatorio.");
            }

            if (string.IsNullOrWhiteSpace(groqApiKey))
            {
                return BadRequest("O header X-Groq-Key e obrigatorio.");
            }

            try
            {
                string cacheKey = $"coin_info_{nameCoin.Trim().ToLowerInvariant()}";

                if (!_cache.TryGetValue(cacheKey, out Dtos.CriptoDto coin))
                {
                    coin = await _criptoService.GetCoinByNameAsync(
                        nameCoin,
                        coinGeckoApiKey,
                        groqApiKey,
                        cancellationToken);

                    if (coin is not null)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                        _cache.Set(cacheKey, coin, cacheEntryOptions);
                    }
                }

                if (coin is null)
                {
                    return NotFound("Moeda nao encontrada.");
                }

                return Ok(coin);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Erro ao consultar os provedores externos: {ex.Message}");
            }
        }
    }
}
