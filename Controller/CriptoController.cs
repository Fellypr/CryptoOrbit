    using CryptoOrbit.Dtos;
    using CryptoOrbit.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;

    namespace CryptoOrbit.Controller
    {
        [ApiController]
        [Route("api/[controller]")]
        public class CriptoController : ControllerBase
        {
            
            private readonly IMemoryCache _cache;

            public CriptoController(IMemoryCache cache)
            {
                
                _cache = cache;
            }

            [HttpGet("get-all-coins")]
            public IActionResult GetAllCoins()
            {
                try
                {
                    if(_cache.TryGetValue("all_cryptos_with_ai", out List<CriptoDto> listaProntaComIa))
                    {
                        return Ok(listaProntaComIa);
                    }
                    return StatusCode(503, "A IA está preparando as análises das moedas. Tente novamente em instantes.");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error ao carregar coin" + ex);

                }
            }
            [HttpGet("{nameCoin}")]
            public IActionResult GetInfoCoin(string nameCoin)
            {
                if (nameCoin == null)
                {
                    return BadRequest("Moeda não encontrada");
                }
                try
                {
                    if (_cache.TryGetValue("all_cryptos_with_ai", out List<CriptoDto> listaCompleta))
                    {
                        var moedaEncontrada = listaCompleta
                                    .FirstOrDefault(c => c.Name.Equals(nameCoin, StringComparison.OrdinalIgnoreCase));

                        if (moedaEncontrada != null)
                        {
                            return Ok(moedaEncontrada);
                        }
                    }
                    return StatusCode(503, "O sistema está inicializando os dados. Tente novamente em instantes.");
                    

                }
                catch (Exception ex)
                {
                    return BadRequest($"Error ao pega a informação da moeda {ex}");
                }
            }
        }
    }