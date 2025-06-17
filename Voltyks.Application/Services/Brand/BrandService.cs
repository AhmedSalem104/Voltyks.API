using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Application.Interfaces.Brand;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.BrandsDTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services
{
    public class BrandService : IBrandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BrandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<IEnumerable<BrandReadDto>>> GetAllAsync()
        {
            try
            {
                var brandRepo = _unitOfWork.GetRepository<Brand, int>();
                var brands = await brandRepo.GetAllAsync();

                var data = brands.Select(b => new BrandReadDto
                {
                    Id = b.Id,
                    Name = b.Name
                });

                return new ApiResponse<IEnumerable<BrandReadDto>>(data, SuccessfulMessage.BrandsRetrievedSuccessfully, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<IEnumerable<BrandReadDto>>(
                    message: ErrorMessages.FailedGetBrands,
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }
    }


}
