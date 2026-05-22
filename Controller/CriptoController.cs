using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoOrbit.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CriptoController : ControllerBase
    {
        private readonly ICripto _criptoService;

        public CriptoController(ICripto criptoService)
        {
            _criptoService = criptoService;
        }

        [HttpGet("get-all-coins")]
        public async Task<IActionResult> GetAllCoins()
        {
            try
            {
                var result = await _criptoService.GetAllCoins();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Error ao carregar coin" + ex);

            }
        }
        [HttpGet("get-info/{nameCoin}")]
        public async Task<IActionResult> GetInfoCoin(string nameCoin)
        {
            if (nameCoin == null)
            {
                return BadRequest("Moeda não encontrada");
            }
            try
            {
                var response = await _criptoService.GetCryptoById(nameCoin);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error ao pega a informação da moeda {ex}");
            }
        }
    }
}