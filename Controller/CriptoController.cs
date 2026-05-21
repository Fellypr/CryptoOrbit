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
            var result = await _criptoService.GetAllCoins();
            return Ok(result);
        }
    }
}