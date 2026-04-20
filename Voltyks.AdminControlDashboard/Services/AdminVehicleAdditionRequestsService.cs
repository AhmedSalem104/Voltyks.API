using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.SignalR;
using Voltyks.Core.Constants;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.Enums;
using Voltyks.Core.Localization;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Persistence.Utilities;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminVehicleAdditionRequestsService : IAdminVehicleAdditionRequestsService
    {
        private readonly VoltyksDbContext _context;
        private readonly ISignalRService _signalRService;
        private readonly IFirebaseService _firebaseService;

        public AdminVehicleAdditionRequestsService(
            VoltyksDbContext context,
            ISignalRService signalRService,
            IFirebaseService firebaseService)
        {
            _context = context;
            _signalRService = signalRService;
            _firebaseService = firebaseService;
        }

        public async Task<ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>> GetAllAsync(
            string? status, PaginationParams? pagination, CancellationToken ct = default)
        {
            try
            {
                pagination ??= new PaginationParams();

                var query = _context.VehicleAdditionRequests
                    .AsNoTracking()
                    .Include(r => r.User)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(r => r.Status == status);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .Select(r => new AdminVehicleAdditionRequestDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserFullName = r.User != null ? (r.User.FullName ?? r.User.UserName ?? "") : "",
                        UserEmail = r.User != null ? r.User.Email : null,
                        UserPhone = r.User != null ? r.User.PhoneNumber : null,
                        BrandName = r.BrandName,
                        ModelName = r.ModelName,
                        Capacity = r.Capacity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ProcessedBy = r.ProcessedBy
                    })
                    .ToListAsync(ct);

                var paged = new PagedResult<AdminVehicleAdditionRequestDto>(
                    items, totalCount, pagination.PageNumber, pagination.PageSize);

                return new ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>(
                    paged, $"Retrieved {items.Count} requests", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>(
                    message: "Failed to retrieve requests",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminVehicleAdditionRequestDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var dto = await _context.VehicleAdditionRequests
                    .AsNoTracking()
                    .Include(r => r.User)
                    .Where(r => r.Id == id)
                    .Select(r => new AdminVehicleAdditionRequestDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserFullName = r.User != null ? (r.User.FullName ?? r.User.UserName ?? "") : "",
                        UserEmail = r.User != null ? r.User.Email : null,
                        UserPhone = r.User != null ? r.User.PhoneNumber : null,
                        BrandName = r.BrandName,
                        ModelName = r.ModelName,
                        Capacity = r.Capacity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ProcessedBy = r.ProcessedBy
                    })
                    .FirstOrDefaultAsync(ct);

                if (dto is null)
                    return new ApiResponse<AdminVehicleAdditionRequestDto>("Request not found", status: false);

                return new ApiResponse<AdminVehicleAdditionRequestDto>(dto, "Request retrieved", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminVehicleAdditionRequestDto>(
                    message: "Failed to retrieve request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AcceptPreviewDto>> GetAcceptPreviewAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var request = await _context.VehicleAdditionRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (request is null)
                    return new ApiResponse<AcceptPreviewDto>("Request not found", status: false);

                var dto = new AcceptPreviewDto
                {
                    Original = new OriginalSubmissionDto
                    {
                        BrandName = request.BrandName,
                        ModelName = request.ModelName,
                        Capacity = request.Capacity
                    }
                };

                var warnings = new List<string>();

                // Parse capacity
                var match = Regex.Match(request.Capacity ?? "", @"[\d]+(\.[\d]+)?");
                if (match.Success && double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedCapacity))
                {
                    dto.ParsedCapacity = parsedCapacity;
                    dto.CapacityParseSuccess = true;
                }
                else
                {
                    dto.CapacityParseSuccess = false;
                    warnings.Add($"Could not parse a numeric capacity from '{request.Capacity}'. Admin must provide a numeric value when accepting.");
                }

                // Load brands + their model counts (small table, safe in-memory)
                var brands = await _context.Brands
                    .AsNoTracking()
                    .Select(b => new { b.Id, b.Name })
                    .ToListAsync(ct);

                var modelCounts = await _context.Models
                    .AsNoTracking()
                    .GroupBy(m => m.BrandId)
                    .Select(g => new { BrandId = g.Key, Count = g.Count() })
                    .ToListAsync(ct);

                var countMap = modelCounts.ToDictionary(x => x.BrandId, x => x.Count);

                var requestedBrand = (request.BrandName ?? "").Trim();

                // Exact brand match (case-insensitive)
                var exact = brands.FirstOrDefault(b =>
                    string.Equals(b.Name, requestedBrand, StringComparison.OrdinalIgnoreCase));

                if (exact != null)
                {
                    dto.ExactBrandMatch = new BrandSuggestionDto
                    {
                        Id = exact.Id,
                        Name = exact.Name,
                        Similarity = 1.0,
                        ModelsCount = countMap.TryGetValue(exact.Id, out var c) ? c : 0
                    };
                }
                else
                {
                    // Fuzzy suggestions (similarity >= 0.6), top 5
                    dto.SimilarBrands = brands
                        .Select(b => new
                        {
                            b.Id,
                            b.Name,
                            Sim = CalculateSimilarity(b.Name, requestedBrand)
                        })
                        .Where(x => x.Sim >= 0.6)
                        .OrderByDescending(x => x.Sim)
                        .Take(5)
                        .Select(x => new BrandSuggestionDto
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Similarity = Math.Round(x.Sim, 2),
                            ModelsCount = countMap.TryGetValue(x.Id, out var c) ? c : 0
                        })
                        .ToList();

                    if (dto.SimilarBrands.Any())
                        warnings.Add($"Found {dto.SimilarBrands.Count} similar brand(s). User may have made a typo.");
                }

                // Model similarity — search within the matched brand (exact or top similar)
                var brandIdsToSearch = new List<int>();
                if (dto.ExactBrandMatch != null) brandIdsToSearch.Add(dto.ExactBrandMatch.Id);
                brandIdsToSearch.AddRange(dto.SimilarBrands.Select(b => b.Id));

                if (brandIdsToSearch.Any())
                {
                    var requestedModel = (request.ModelName ?? "").Trim();
                    var candidateModels = await _context.Models
                        .AsNoTracking()
                        .Where(m => brandIdsToSearch.Contains(m.BrandId))
                        .Select(m => new
                        {
                            m.Id,
                            m.Name,
                            m.BrandId,
                            BrandName = m.Brand!.Name
                        })
                        .ToListAsync(ct);

                    var exactModel = candidateModels.FirstOrDefault(m =>
                        string.Equals(m.Name, requestedModel, StringComparison.OrdinalIgnoreCase));

                    if (exactModel != null)
                    {
                        dto.ExactModelMatch = new ModelSuggestionDto
                        {
                            ModelId = exactModel.Id,
                            ModelName = exactModel.Name,
                            BrandId = exactModel.BrandId,
                            BrandName = exactModel.BrandName,
                            Similarity = 1.0
                        };
                        warnings.Add($"A model named '{exactModel.Name}' already exists under '{exactModel.BrandName}'. Accepting would create a duplicate.");
                    }
                    else
                    {
                        dto.SimilarModels = candidateModels
                            .Select(m => new
                            {
                                m.Id,
                                m.Name,
                                m.BrandId,
                                m.BrandName,
                                Sim = CalculateSimilarity(m.Name, requestedModel)
                            })
                            .Where(x => x.Sim >= 0.6)
                            .OrderByDescending(x => x.Sim)
                            .Take(5)
                            .Select(x => new ModelSuggestionDto
                            {
                                ModelId = x.Id,
                                ModelName = x.Name,
                                BrandId = x.BrandId,
                                BrandName = x.BrandName,
                                Similarity = Math.Round(x.Sim, 2)
                            })
                            .ToList();
                    }
                }

                dto.Warnings = warnings;
                return new ApiResponse<AcceptPreviewDto>(dto, "Preview generated", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AcceptPreviewDto>(
                    message: "Failed to generate preview",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> AcceptAsync(int id, string adminId, AcceptVehicleAdditionRequestDto? overrides, CancellationToken ct = default)
        {
            using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var request = await _context.VehicleAdditionRequests
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (request is null)
                    return new ApiResponse<object>("Request not found", status: false);

                if (request.Status != VehicleAdditionRequestStatuses.Pending)
                    return new ApiResponse<object>(
                        $"Request is already {request.Status}, cannot accept it.", status: false);

                // Resolve final values (admin overrides > original submission)
                var finalBrandName = !string.IsNullOrWhiteSpace(overrides?.BrandName)
                    ? overrides.BrandName.Trim()
                    : request.BrandName.Trim();

                var finalModelName = !string.IsNullOrWhiteSpace(overrides?.ModelName)
                    ? overrides.ModelName.Trim()
                    : request.ModelName.Trim();

                // Resolve the Brand: existing ID wins, then override name, then original
                Brand? brand = null;
                if (overrides?.UseExistingBrandId.HasValue == true)
                {
                    brand = await _context.Brands
                        .FirstOrDefaultAsync(b => b.Id == overrides.UseExistingBrandId.Value, ct);

                    if (brand is null)
                        return new ApiResponse<object>(
                            $"Brand with id {overrides.UseExistingBrandId} not found.", status: false);
                }
                else
                {
                    var brandLower = finalBrandName.ToLower();
                    brand = await _context.Brands
                        .FirstOrDefaultAsync(b => b.Name.ToLower() == brandLower, ct);

                    if (brand is null)
                    {
                        brand = new Brand { Name = finalBrandName };
                        _context.Brands.Add(brand);
                        await _context.SaveChangesAsync(ct);
                    }
                }

                // Check model dedup against the resolved brand
                var modelLower = finalModelName.ToLower();
                var modelExists = await _context.Models
                    .AnyAsync(m => m.BrandId == brand.Id && m.Name.ToLower() == modelLower, ct);

                if (modelExists)
                    return new ApiResponse<object>(
                        $"Model '{finalModelName}' already exists under brand '{brand.Name}'. Please decline this request.",
                        status: false);

                // Resolve capacity: admin override takes precedence, otherwise parse
                double capacityValue;
                if (overrides?.Capacity.HasValue == true)
                {
                    if (overrides.Capacity.Value <= 0)
                        return new ApiResponse<object>(
                            "Capacity must be greater than zero.", status: false);
                    capacityValue = overrides.Capacity.Value;
                }
                else
                {
                    var match = Regex.Match(request.Capacity ?? "", @"[\d]+(\.[\d]+)?");
                    if (!match.Success || !double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out capacityValue))
                        return new ApiResponse<object>(
                            $"Invalid capacity format: '{request.Capacity}'. Please provide a numeric capacity in the accept payload.",
                            status: false);
                }

                // Create Model
                var model = new Model
                {
                    Name = finalModelName,
                    BrandId = brand.Id,
                    Capacity = capacityValue
                };
                _context.Models.Add(model);
                await _context.SaveChangesAsync(ct);

                // Update request status
                request.Status = VehicleAdditionRequestStatuses.Accepted;
                request.UpdatedAt = DateTimeHelper.GetEgyptTime();
                request.ProcessedBy = adminId;
                await _context.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                // Notify user (DB + FCM + SignalR)
                var lang = Languages.Normalize(overrides?.Lang);
                var (title, body) = NotificationMessages.VehicleAdditionAccepted(lang);
                await NotifyUserAsync(
                    userId: request.UserId,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    type: NotificationTypes.VehicleAdditionRequest_Accepted,
                    ct: ct);

                return new ApiResponse<object>(
                    new { requestId = request.Id, brandId = brand.Id, modelId = model.Id },
                    "Request accepted successfully. Vehicle added to the system.",
                    true);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>(
                    message: "Failed to accept request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> DeclineAsync(int id, string adminId, DeclineVehicleAdditionRequestDto? body, CancellationToken ct = default)
        {
            try
            {
                var request = await _context.VehicleAdditionRequests
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (request is null)
                    return new ApiResponse<object>("Request not found", status: false);

                if (request.Status != VehicleAdditionRequestStatuses.Pending)
                    return new ApiResponse<object>(
                        $"Request is already {request.Status}, cannot decline it.", status: false);

                request.Status = VehicleAdditionRequestStatuses.Declined;
                request.UpdatedAt = DateTimeHelper.GetEgyptTime();
                request.ProcessedBy = adminId;
                await _context.SaveChangesAsync(ct);

                // Notify user (DB + FCM + SignalR)
                var lang = Languages.Normalize(body?.Lang);
                var (title, msgBody) = NotificationMessages.VehicleAdditionDeclined(lang);
                await NotifyUserAsync(
                    userId: request.UserId,
                    requestId: request.Id,
                    title: title,
                    body: msgBody,
                    type: NotificationTypes.VehicleAdditionRequest_Declined,
                    ct: ct);

                return new ApiResponse<object>(
                    new { requestId = request.Id },
                    "Request declined successfully.",
                    true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to decline request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        private async Task NotifyUserAsync(
            string userId, int requestId, string title, string body, string type, CancellationToken ct)
        {
            // 1) Persist to DB
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                OriginalId = requestId,
                UserTypeId = 2, // VehicleOwner
                IsAdminNotification = false,
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime(),
                RelatedRequestId = null
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);

            // 2) FCM push (works even if app is closed)
            try
            {
                var tokens = await _context.Set<DeviceToken>()
                    .AsNoTracking()
                    .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Token))
                    .Select(t => t.Token)
                    .ToListAsync(ct);

                if (tokens.Count > 0)
                {
                    var extraData = new Dictionary<string, string>
                    {
                        ["vehicleAdditionRequestId"] = requestId.ToString(),
                        ["NotificationType"] = type,
                        ["userRole"] = "vehicle_owner"
                    };

                    await Task.WhenAll(tokens.Select(t =>
                        _firebaseService.SendNotificationAsync(t, title, body, requestId, type, extraData)
                    ));
                }
            }
            catch
            {
                // FCM failure shouldn't block the accept/decline flow
            }

            // 3) Real-time via SignalR (if user connected)
            try
            {
                await _signalRService.SendNotificationAsync(userId, title, body, new
                {
                    id = notification.Id,
                    type,
                    vehicleAdditionRequestId = requestId,
                    userRole = "vehicle_owner"
                }, ct);
            }
            catch
            {
                // SignalR failure shouldn't block the accept/decline flow
            }
        }

        private static double CalculateSimilarity(string a, string b)
        {
            a = (a ?? "").Trim().ToLowerInvariant();
            b = (b ?? "").Trim().ToLowerInvariant();
            if (a.Length == 0 && b.Length == 0) return 1.0;
            if (a == b) return 1.0;
            int maxLen = Math.Max(a.Length, b.Length);
            if (maxLen == 0) return 1.0;
            int distance = LevenshteinDistance(a, b);
            return 1.0 - (double)distance / maxLen;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            return d[a.Length, b.Length];
        }
    }
}
