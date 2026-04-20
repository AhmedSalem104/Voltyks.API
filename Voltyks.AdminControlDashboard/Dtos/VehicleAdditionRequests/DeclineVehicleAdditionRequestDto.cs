using Voltyks.Core.DTOs.Common;

namespace Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests
{
    /// <summary>
    /// Optional payload for declining a request. Currently only carries the
    /// language for the user notification.
    /// </summary>
    public class DeclineVehicleAdditionRequestDto : ILocalizedRequest
    {
        /// <summary>Optional language for the user notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }
}
