using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Core.DTOs.FeesConfig;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/config/fees")]
    public class FeesConfigController : ControllerBase
    {
        private readonly IFeesConfigService _service;

        public FeesConfigController(IFeesConfigService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var res = await _service.GetAsync();
            return res.Status ? Ok(res) : BadRequest(res);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")] // أبقيها لو عندك Role=Admin؛ وإلا شيلها مؤقتًا للاختبار
        public async Task<IActionResult> Update([FromBody] FeesConfigUpdateDto dto)
        {
            var res = await _service.UpdateAsync(dto);
            return res.Status ? Ok(res) : BadRequest(res);
        }
    }
}
