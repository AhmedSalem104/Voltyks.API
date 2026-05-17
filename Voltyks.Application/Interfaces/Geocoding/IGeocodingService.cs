using System.Threading;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Geocoding
{
    public interface IGeocodingService
    {
        Task<(string Area, string Street)> GetAddressAsync(
            double latitude, double longitude, CancellationToken ct = default);
    }
}
