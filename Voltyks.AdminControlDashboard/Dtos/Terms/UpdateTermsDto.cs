using System.Text.Json;

namespace Voltyks.AdminControlDashboard.Dtos.Terms
{
    public class UpdateTermsDto
    {
        public string Lang { get; set; } = "en";
        public JsonElement Content { get; set; }
    }
}
