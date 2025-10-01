using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Core.DTOs.FeesConfig;
using Voltyks.Core.DTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Main;
using FeesConfigEntity = Voltyks.Persistence.Entities.Main.FeesConfig;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace Voltyks.Application.Services.FeesConfig
{

    public class FeesConfigService : IFeesConfigService
    {
        private const int SINGLE_ROW_ID = 1;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContext;

        public FeesConfigService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContext = httpContext;
        }

        public async Task<ApiResponse<FeesConfigDto>> GetAsync()
        {
            var repo = _unitOfWork.GetRepository<FeesConfigEntity, int>();
            var row = await repo.GetFirstOrDefaultAsync(x => x.Id == SINGLE_ROW_ID);

            if (row is null)
            {
                row = new FeesConfigEntity
                {
                    Id = SINGLE_ROW_ID,
                    MinimumFee = 40m,
                    Percentage = 10m,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                };
                await repo.AddAsync(row);
                await _unitOfWork.SaveChangesAsync();
            }

            var dto = _mapper.Map<FeesConfigDto>(row);
            return new ApiResponse<FeesConfigDto>(dto, "Fees configuration retrieved successfully", status: true);
        }

        public async Task<ApiResponse<FeesConfigDto>> UpdateAsync(FeesConfigUpdateDto dto)
        {
            if (dto.MinimumFee < 0)
                return new ApiResponse<FeesConfigDto>(message: "MinimumFee must be >= 0", status: false);

            if (dto.Percentage < 0 || dto.Percentage > 100)
                return new ApiResponse<FeesConfigDto>(message: "Percentage must be between 0 and 100", status: false);

            var repo = _unitOfWork.GetRepository<FeesConfigEntity, int>();
            var row = await repo.GetFirstOrDefaultAsync(x => x.Id == SINGLE_ROW_ID);

            var updatedBy = GetCurrentUserName() ?? GetCurrentUserId() ?? "system";
            var nowUtc = DateTime.UtcNow;

            if (row is null)
            {
                row = new FeesConfigEntity
                {
                    Id = SINGLE_ROW_ID,
                    MinimumFee = dto.MinimumFee,
                    Percentage = dto.Percentage,
                    UpdatedAt = nowUtc,
                    UpdatedBy = updatedBy
                };
                await repo.AddAsync(row);
            }
            else
            {
                row.MinimumFee = dto.MinimumFee;
                row.Percentage = dto.Percentage;
                row.UpdatedAt = nowUtc;
                row.UpdatedBy = updatedBy;
                repo.Update(row);
            }

            await _unitOfWork.SaveChangesAsync();

            var outDto = _mapper.Map<FeesConfigDto>(row);
            return new ApiResponse<FeesConfigDto>(outDto, "Fees configuration updated successfully", status: true);
        }

        private string? GetCurrentUserName()
            => _httpContext.HttpContext?.User?.Identity?.Name;

        private string? GetCurrentUserId()
        {
            var u = _httpContext.HttpContext?.User;
            if (u?.Identity?.IsAuthenticated != true) return null;

            return u.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? u.FindFirst("sub")?.Value
                ?? u.FindFirst("uid")?.Value
                ?? u.FindFirst("user_id")?.Value
                ?? u.FindFirst("id")?.Value;
        }
    }
}
