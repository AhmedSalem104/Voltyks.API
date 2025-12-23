using Voltyks.Application.Interfaces.MobileAppConfig;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.MobileAppConfig;
using Voltyks.Infrastructure.UnitOfWork;
using MobileAppConfigEntity = Voltyks.Persistence.Entities.Main.MobileAppConfig;

namespace Voltyks.Application.Services.MobileAppConfig
{
    public class MobileAppConfigService : IMobileAppConfigService
    {
        private const int SINGLE_ROW_ID = 1;
        private readonly IUnitOfWork _unitOfWork;

        public MobileAppConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<MobileAppStatusDto>> GetStatusAsync(string? platform, string? version)
        {
            var config = await GetOrCreateConfigAsync();

            // Determine which platform to check
            bool isEnabled;
            string? minVersion;

            if (string.Equals(platform, "android", StringComparison.OrdinalIgnoreCase))
            {
                isEnabled = config.AndroidEnabled;
                minVersion = config.AndroidMinVersion;
            }
            else if (string.Equals(platform, "ios", StringComparison.OrdinalIgnoreCase))
            {
                isEnabled = config.IosEnabled;
                minVersion = config.IosMinVersion;
            }
            else
            {
                // No platform specified - return combined status
                isEnabled = config.AndroidEnabled && config.IosEnabled;
                minVersion = null;
            }

            // Check version validity
            bool isVersionValid = IsVersionValid(version, minVersion);

            var dto = new MobileAppStatusDto
            {
                IsEnabled = isEnabled,
                IsVersionValid = isVersionValid,
                MinVersion = minVersion
            };

            return new ApiResponse<MobileAppStatusDto>(dto, "Status retrieved successfully", status: true);
        }

        public async Task<ApiResponse<MobileAppEnabledDto>> GetLegacyStatusAsync()
        {
            var config = await GetOrCreateConfigAsync();

            // Legacy: returns true only if BOTH platforms are enabled
            var dto = new MobileAppEnabledDto
            {
                MobileAppEnabled = config.AndroidEnabled && config.IosEnabled
            };

            return new ApiResponse<MobileAppEnabledDto>(dto, "Status retrieved successfully", status: true);
        }

        public async Task<ApiResponse<MobileAppConfigAdminDto>> GetAdminConfigAsync()
        {
            var config = await GetOrCreateConfigAsync();

            var dto = new MobileAppConfigAdminDto
            {
                AndroidEnabled = config.AndroidEnabled,
                IosEnabled = config.IosEnabled,
                AndroidMinVersion = config.AndroidMinVersion,
                IosMinVersion = config.IosMinVersion
            };

            return new ApiResponse<MobileAppConfigAdminDto>(dto, "Configuration retrieved successfully", status: true);
        }

        public async Task<ApiResponse<MobileAppConfigAdminDto>> UpdateAsync(UpdateMobileAppConfigDto dto)
        {
            var repo = _unitOfWork.GetRepository<MobileAppConfigEntity, int>();
            var row = await repo.GetFirstOrDefaultAsync(x => x.Id == SINGLE_ROW_ID);

            if (row is null)
            {
                row = new MobileAppConfigEntity
                {
                    Id = SINGLE_ROW_ID,
                    AndroidEnabled = dto.AndroidEnabled,
                    IosEnabled = dto.IosEnabled,
                    AndroidMinVersion = dto.AndroidMinVersion,
                    IosMinVersion = dto.IosMinVersion
                };
                await repo.AddAsync(row);
            }
            else
            {
                row.AndroidEnabled = dto.AndroidEnabled;
                row.IosEnabled = dto.IosEnabled;
                row.AndroidMinVersion = dto.AndroidMinVersion;
                row.IosMinVersion = dto.IosMinVersion;
                repo.Update(row);
            }

            await _unitOfWork.SaveChangesAsync();

            var outDto = new MobileAppConfigAdminDto
            {
                AndroidEnabled = row.AndroidEnabled,
                IosEnabled = row.IosEnabled,
                AndroidMinVersion = row.AndroidMinVersion,
                IosMinVersion = row.IosMinVersion
            };

            return new ApiResponse<MobileAppConfigAdminDto>(outDto, "Configuration updated successfully", status: true);
        }

        private async Task<MobileAppConfigEntity> GetOrCreateConfigAsync()
        {
            var repo = _unitOfWork.GetRepository<MobileAppConfigEntity, int>();
            var row = await repo.GetFirstOrDefaultAsync(x => x.Id == SINGLE_ROW_ID);

            if (row is null)
            {
                row = new MobileAppConfigEntity
                {
                    Id = SINGLE_ROW_ID,
                    AndroidEnabled = true,
                    IosEnabled = true
                };
                await repo.AddAsync(row);
                await _unitOfWork.SaveChangesAsync();
            }

            return row;
        }

        /// <summary>
        /// Compare client version with minimum required version
        /// Returns true if client >= min (or if either is null/empty)
        /// </summary>
        private static bool IsVersionValid(string? clientVersion, string? minVersion)
        {
            // If no min version set, always valid
            if (string.IsNullOrWhiteSpace(minVersion)) return true;

            // If client didn't send version, assume valid
            if (string.IsNullOrWhiteSpace(clientVersion)) return true;

            try
            {
                var clientParts = ParseVersion(clientVersion);
                var minParts = ParseVersion(minVersion);

                return CompareVersions(clientParts, minParts) >= 0;
            }
            catch
            {
                // If parsing fails, assume valid
                return true;
            }
        }

        /// <summary>
        /// Parse version string "1.2.3" into array [1, 2, 3]
        /// </summary>
        private static int[] ParseVersion(string version)
        {
            return version.Split('.').Select(int.Parse).ToArray();
        }

        /// <summary>
        /// Compare two version arrays
        /// Returns: positive if a > b, negative if a < b, 0 if equal
        /// </summary>
        private static int CompareVersions(int[] a, int[] b)
        {
            int maxLen = Math.Max(a.Length, b.Length);

            for (int i = 0; i < maxLen; i++)
            {
                int aVal = i < a.Length ? a[i] : 0;
                int bVal = i < b.Length ? b[i] : 0;

                if (aVal > bVal) return 1;
                if (aVal < bVal) return -1;
            }

            return 0;
        }
    }
}
