using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Voltyks.Application.ServicesManager.ServicesManager;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class BrandController : ControllerBase
    {
        private readonly IServiceManager _service;

        public BrandController(IServiceManager service)
        {
            _service = service;
        }

        [HttpGet("GetAllBrands")]
        [ResponseCache(Duration = 3600)] // 1 hour HTTP cache header for downstream proxies
        [OutputCache(PolicyName = "StaticData")] // 30 min server-side cache
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.BrandService.GetAllAsync();
            return result.Status ? Ok(result) : StatusCode(500, result);
        }
    }
}
