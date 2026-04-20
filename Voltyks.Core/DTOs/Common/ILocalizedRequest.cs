namespace Voltyks.Core.DTOs.Common
{
    /// <summary>
    /// Marker interface for request DTOs that trigger user-facing notifications.
    /// Carrying the optional language the caller wants the notification delivered in.
    /// Accepted values: "en" (default) / "ar". Anything else falls back to English.
    /// </summary>
    public interface ILocalizedRequest
    {
        string? Lang { get; set; }
    }
}
