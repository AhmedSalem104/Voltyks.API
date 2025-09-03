using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Google;
using Voltyks.Core.DTOs.ModelDTOs;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using System.Data.Entity;
using Voltyks.Persistence.Entities;
using Voltyks.Application.Interfaces;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services
{

    public class ModelService(IUnitOfWork _unitOfWork , IMapper _mapper) : IModelService
    {
        public async Task<ApiResponse<IEnumerable<ModelDto>>> GetModelsByBrandIdAsync(int brandId)
        {
            var models = await _unitOfWork
                                    .GetRepository<Model, int>()
                                    .GetAllWithIncludeAsync(
                                        filter: m => m.BrandId == brandId,
                                        includes: m => m.Brand
                                    );

            if (!models.Any())
            {
                return new ApiResponse<IEnumerable<ModelDto>>(ErrorMessages.NoModelsFoundForThisBrand, false);
            }

            var modelDtos = _mapper.Map<IEnumerable<ModelDto>>(models);

            return new ApiResponse<IEnumerable<ModelDto>>(modelDtos, SuccessfulMessage.ModelsRetrievedSuccessfully, true);
        }
        public async Task<ApiResponse<IEnumerable<int>>> GetYearsByModelIdAsync(int modelId)
        {
            var vehicles = await _unitOfWork
                .GetRepository<Vehicle, int>()
                .GetAllAsync(v => v.ModelId == modelId);

            var years = vehicles
                .Select(v => v.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            if (!years.Any())
                return new ApiResponse<IEnumerable<int>>(ErrorMessages.NoYearsFoundForThisModel, false);

            return new ApiResponse<IEnumerable<int>>(years, SuccessfulMessage.YearsRetrievedSuccessfully, true);
        }

    }




}
