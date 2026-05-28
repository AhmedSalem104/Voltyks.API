using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.ModelDTOs;

namespace Voltyks.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ModelController(IServiceManager _serviceManager) : ControllerBase
    {
        [HttpGet("GetModelsByBrandId")]
        [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "brandId" })] // 1 hour HTTP cache header
        [OutputCache(PolicyName = "StaticData")] // 30 min server-side cache, varies by query
        public async Task<IActionResult> GetModelsByBrandId(int brandId)
        {
            var response = await _serviceManager.ModelService.GetModelsByBrandIdAsync(brandId);
            return Ok(response);
        }

        [HttpGet("GetYearsByModelId")]
        [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "modelId" })] // 1 hour HTTP cache header
        [OutputCache(PolicyName = "StaticData")] // 30 min server-side cache, varies by query
        public async Task<IActionResult> GetYearsByModelId(int modelId)
        {
            var response = await _serviceManager.ModelService.GetYearsByModelIdAsync(modelId);
            return Ok(response);
        }

    }
}
