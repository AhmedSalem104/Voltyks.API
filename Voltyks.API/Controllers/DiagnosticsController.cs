using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.Telemetry;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("diagnostics")]
    [Authorize(Roles = "Admin")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IFcmTelemetry _fcmTelemetry;

        public DiagnosticsController(IFcmTelemetry fcmTelemetry)
        {
            _fcmTelemetry = fcmTelemetry;
        }

        [HttpGet("fcm-stats")]
        public ActionResult<FcmTelemetrySnapshot> GetFcmStats()
            => Ok(_fcmTelemetry.GetSnapshot());
    }
}
