namespace Voltyks.Core.DTOs.Process
{
    public class PendingProcessesResponseDto
    {
        public int Count { get; set; }
        public List<PendingProcessDto> Processes { get; set; } = new();
    }
}
