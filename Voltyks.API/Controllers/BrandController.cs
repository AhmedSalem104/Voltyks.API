using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandController : ControllerBase
    {
        private readonly IServiceManager _service;

        public BrandController(IServiceManager service)
        {
            _service = service;
        }

        [HttpGet("GetAllBrands")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.BrandService.GetAllAsync();
            return result.Status ? Ok(result) : StatusCode(500, result);
        }
    }
}
