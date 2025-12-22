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

        public async Task<ApiResponse<MobileAppStatusDto>> GetStatusAsync()
        {
            var repo = _unitOfWork.GetRepository<MobileAppConfigEntity, int>();
            var row = await repo.GetFirstOrDefaultAsync(x => x.Id == SINGLE_ROW_ID);

            if (row is null)
            {
                row = new MobileAppConfigEntity
                {
                    Id = SINGLE_ROW_ID,
                    MobileAppEnabled = true
                };
                await repo.AddAsync(row);
                await _unitOfWork.SaveChangesAsync();
            }

            var dto = new MobileAppStatusDto
            {
                MobileAppEnabled = row.MobileAppEnabled
            };

            return new ApiResponse<MobileAppStatusDto>(dto, "Status retrieved successfully", status: true);
        }

        public async Task<ApiResponse<MobileAppStatusDto>> UpdateAsync(UpdateMobileAppConfigDto dto)
        {
            var repo = _unitOfWork.GetRepository<MobileAppConfigEntity, int>();
            var row = await repo.GetFirstOrDefaultAsync(x => x.Id == SINGLE_ROW_ID);

            if (row is null)
            {
                row = new MobileAppConfigEntity
                {
                    Id = SINGLE_ROW_ID,
                    MobileAppEnabled = dto.MobileAppEnabled
                };
                await repo.AddAsync(row);
            }
            else
            {
                row.MobileAppEnabled = dto.MobileAppEnabled;
                repo.Update(row);
            }

            await _unitOfWork.SaveChangesAsync();

            var outDto = new MobileAppStatusDto
            {
                MobileAppEnabled = row.MobileAppEnabled
            };

            return new ApiResponse<MobileAppStatusDto>(outDto, "Configuration updated successfully", status: true);
        }
    }
}
