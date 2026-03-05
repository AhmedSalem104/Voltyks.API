namespace Voltyks.Application.Interfaces.Processes
{
    public interface IRatingWindowProcessor
    {
        Task ProcessExpiredWindowsAsync(CancellationToken ct);
    }
}
