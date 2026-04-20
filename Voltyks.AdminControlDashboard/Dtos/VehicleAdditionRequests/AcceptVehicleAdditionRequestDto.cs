using Voltyks.Core.DTOs.Common;

namespace Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests
{
    /// <summary>
    /// Optional payload for accepting a request. When null/omitted, the service
    /// uses the original submitted values. When provided, fields override them.
    /// </summary>
    public class AcceptVehicleAdditionRequestDto : ILocalizedRequest
    {
        /// <summary>Optional language for the user notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }


        /// <summary>
        /// If set, link the new model to this existing brand ID (no new brand created).
        /// Takes precedence over <see cref="BrandName"/>.
        /// </summary>
        public int? UseExistingBrandId { get; set; }

        /// <summary>
        /// Override the brand name. If a brand with this name exists (case-insensitive)
        /// it will be reused; otherwise a new brand is created. Ignored if
        /// <see cref="UseExistingBrandId"/> is set.
        /// </summary>
        public string? BrandName { get; set; }

        /// <summary>Override the model name.</summary>
        public string? ModelName { get; set; }

        /// <summary>Override the capacity (in kWh). Skips the string parser.</summary>
        public double? Capacity { get; set; }
    }
}
