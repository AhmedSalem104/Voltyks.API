using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.ModelDTOs;

namespace Voltyks.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelController(IServiceManager _serviceManager) : ControllerBase
    {
        [HttpGet("GetModelsByBrandId")]
        [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "brandId" })] // 1 hour cache per brandId
        public async Task<IActionResult> GetModelsByBrandId(int brandId)
        {
            var response = await _serviceManager.ModelService.GetModelsByBrandIdAsync(brandId);
            return Ok(response);
        }

        [HttpGet("GetYearsByModelId")]
        [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "modelId" })] // 1 hour cache per modelId
        public async Task<IActionResult> GetYearsByModelId(int modelId)
        {
            var response = await _serviceManager.ModelService.GetYearsByModelIdAsync(modelId);
            return Ok(response);
        }

    }
}
