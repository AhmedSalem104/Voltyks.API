using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Process;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminProcessService : IAdminProcessService
    {
        private readonly VoltyksDbContext _context;

        public AdminProcessService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminProcessDto>>> GetProcessesAsync(CancellationToken ct = default)
        {
            var processes = await _context.Process
                .Include(p => p.ChargerRequest)
                    .ThenInclude(cr => cr!.Charger)
                        .ThenInclude(c => c.Protocol)
                .Include(p => p.ChargerRequest)
                    .ThenInclude(cr => cr!.Charger)
                        .ThenInclude(c => c.Capacity)
                .Include(p => p.ChargerRequest)
                    .ThenInclude(cr => cr!.Charger)
                        .ThenInclude(c => c.PriceOption)
                .Include(p => p.ChargerRequest)
                    .ThenInclude(cr => cr!.Charger)
                        .ThenInclude(c => c.Address)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync(ct);

            // Get all unique user IDs
            var vehicleOwnerIds = processes.Select(p => p.VehicleOwnerId).Distinct().ToList();
            var chargerOwnerIds = processes.Select(p => p.ChargerOwnerId).Distinct().ToList();
            var allUserIds = vehicleOwnerIds.Union(chargerOwnerIds).ToList();

            // Fetch users
            var users = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .Cast<AppUser>()
                .ToDictionaryAsync(u => u.Id, ct);

            // Fetch vehicles for vehicle owners
            var vehiclesList = await _context.Vehicles
                .Include(v => v.Brand)
                .Include(v => v.Model)
                .Where(v => vehicleOwnerIds.Contains(v.UserId) && !v.IsDeleted)
                .ToListAsync(ct);

            var vehicles = vehiclesList
                .GroupBy(v => v.UserId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());

            var result = processes.Select(p =>
            {
                users.TryGetValue(p.VehicleOwnerId, out var vehicleOwner);
                users.TryGetValue(p.ChargerOwnerId, out var chargerOwner);
                vehicles.TryGetValue(p.VehicleOwnerId, out var vehicle);
                var cr = p.ChargerRequest;
                var charger = cr?.Charger;

                return new AdminProcessDto
                {
                    // Process Info
                    Id = p.Id,
                    ChargerRequestId = p.ChargerRequestId,
                    Status = p.Status.ToString(),
                    DateCreated = p.DateCreated,
                    DateCompleted = p.DateCompleted,
                    EstimatedPrice = p.EstimatedPrice,
                    AmountPaid = p.AmountPaid,
                    AmountCharged = p.AmountCharged,
                    VehicleOwnerRating = p.VehicleOwnerRating,
                    ChargerOwnerRating = p.ChargerOwnerRating,

                    // Vehicle Owner
                    VehicleOwnerId = p.VehicleOwnerId,
                    VehicleOwnerName = vehicleOwner?.FullName ?? "",
                    VehicleOwnerEmail = vehicleOwner?.Email ?? "",
                    VehicleOwnerPhone = vehicleOwner?.PhoneNumber ?? "",
                    VehicleOwnerWallet = vehicleOwner?.Wallet ?? 0,
                    VehicleOwnerUserRating = vehicleOwner?.Rating ?? 0,
                    VehicleOwnerIsBanned = vehicleOwner?.IsBanned ?? false,

                    // Charger Owner
                    ChargerOwnerId = p.ChargerOwnerId,
                    ChargerOwnerName = chargerOwner?.FullName ?? "",
                    ChargerOwnerEmail = chargerOwner?.Email ?? "",
                    ChargerOwnerPhone = chargerOwner?.PhoneNumber ?? "",
                    ChargerOwnerWallet = chargerOwner?.Wallet ?? 0,
                    ChargerOwnerUserRating = chargerOwner?.Rating ?? 0,
                    ChargerOwnerIsBanned = chargerOwner?.IsBanned ?? false,

                    // Charging Request
                    RequestStatus = cr?.Status ?? "",
                    KwNeeded = cr?.KwNeeded ?? 0,
                    CurrentBatteryPercentage = cr?.CurrentBatteryPercentage ?? 0,
                    RequestLatitude = cr?.Latitude ?? 0,
                    RequestLongitude = cr?.Longitude ?? 0,
                    RequestedAt = cr?.RequestedAt ?? DateTime.MinValue,
                    RespondedAt = cr?.RespondedAt,
                    ConfirmedAt = cr?.ConfirmedAt,
                    BaseAmount = cr?.BaseAmount ?? 0,
                    VoltyksFees = cr?.VoltyksFees ?? 0,
                    RequestEstimatedPrice = cr?.EstimatedPrice ?? 0,

                    // Charger
                    ChargerId = charger?.Id ?? 0,
                    ChargerProtocol = charger?.Protocol?.Name ?? "",
                    ChargerCapacityKw = charger?.Capacity?.kw ?? 0,
                    ChargerPrice = charger?.PriceOption?.Value ?? 0,
                    ChargerHasAdaptor = charger?.Adaptor ?? false,
                    ChargerRating = charger?.AverageRating ?? 0,
                    ChargerIsActive = charger?.IsActive ?? false,

                    // Charger Address
                    ChargerArea = charger?.Address?.Area ?? "",
                    ChargerStreet = charger?.Address?.Street ?? "",
                    ChargerBuildingNumber = charger?.Address?.BuildingNumber ?? "",
                    ChargerLatitude = charger?.Address?.Latitude ?? 0,
                    ChargerLongitude = charger?.Address?.Longitude ?? 0,

                    // Vehicle
                    VehicleBrand = vehicle?.Brand?.Name ?? "",
                    VehicleModel = vehicle?.Model?.Name ?? "",
                    VehicleColor = vehicle?.Color ?? "",
                    VehiclePlate = vehicle?.Plate ?? "",
                    VehicleYear = vehicle?.Year ?? 0,
                    VehicleCapacity = vehicle?.Model?.Capacity ?? 0
                };
            }).ToList();

            return new ApiResponse<List<AdminProcessDto>>(
                data: result,
                message: $"Retrieved {result.Count} processes",
                status: true
            );
        }
    }
}
