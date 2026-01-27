namespace Voltyks.Core.DTOs.Process
{
    public class ResumeContext
    {
        public string ScreenKey { get; set; } = string.Empty;
        public Dictionary<string, object> Params { get; set; } = new();
    }
}
