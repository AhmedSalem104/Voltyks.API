using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/protocol")]
    public class AdminProtocolController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminProtocolController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/protocol (Read-only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProtocol(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminProtocolService.GetProtocolAsync(ct);
            return Ok(result);
        }
    }
}
