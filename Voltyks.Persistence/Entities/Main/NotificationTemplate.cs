using System.ComponentModel.DataAnnotations;
using Voltyks.Persistence.Utilities;

namespace Voltyks.Persistence.Entities.Main
{
    /// <summary>
    /// Editable notification template. The template Key matches the corresponding
    /// helper in <see cref="Voltyks.Core.Localization.NotificationMessages"/> — when
    /// a row is missing the resolver falls back to the hardcoded helper, so a clean
    /// install with an empty table still produces the original messages.
    /// </summary>
    public class NotificationTemplate
    {
        [Key]
        [MaxLength(120)]
        public string Key { get; set; } = default!;

        [MaxLength(255)]
        public string TitleEn { get; set; } = default!;

        [MaxLength(255)]
        public string TitleAr { get; set; } = default!;

        [MaxLength(2000)]
        public string BodyEn { get; set; } = default!;

        [MaxLength(2000)]
        public string BodyAr { get; set; } = default!;

        /// <summary>JSON array of placeholder names, e.g. <c>["stationOwnerName"]</c>.</summary>
        [MaxLength(1000)]
        public string RequiredParamsJson { get; set; } = "[]";

        public bool IsCustomizable { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTimeHelper.GetEgyptTime();
        public string? UpdatedBy { get; set; }
    }
}
