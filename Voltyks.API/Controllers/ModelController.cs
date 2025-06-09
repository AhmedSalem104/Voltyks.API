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
        public async Task<IActionResult> GetModelsByBrandId(int brandId)
        {
            var response = await _serviceManager.ModelService.GetModelsByBrandIdAsync(brandId);
            return Ok(response);
        }

        [HttpGet("GetYearsByModelId")]
        public async Task<IActionResult> GetYearsByModelId(int modelId)
        {
            var response = await _serviceManager.ModelService.GetYearsByModelIdAsync(modelId);
            return Ok(response);
        }

    }




}
