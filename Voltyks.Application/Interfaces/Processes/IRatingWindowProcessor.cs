namespace Voltyks.Application.Interfaces.Processes
{
    public record RatingProcessingResult(int ExpiredProcessed, int StuckFinalized);

    public interface IRatingWindowProcessor
    {
        Task<RatingProcessingResult> ProcessExpiredWindowsAsync(CancellationToken ct);
    }
}
