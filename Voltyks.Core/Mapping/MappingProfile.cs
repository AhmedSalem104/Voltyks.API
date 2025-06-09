
using Voltyks.Core.DTOs.ModelDTOs;
using AutoMapper;

using Voltyks.Core.DTOs.BrandsDTOs;

using Voltyks.Persistence.Entities.Main;
using Model = Voltyks.Persistence.Entities.Main.Model;
using Voltyks.Core.DTOs.VehicleDTOs;


namespace Voltyks.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Model, ModelDto>()
                .ForMember(dest => dest.ModelId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.Name));

            CreateMap<Brand, BrandDto>();
           
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.Model.Name))
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand.Name))
                .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.Year));

            CreateMap<VehicleDto, Vehicle>().ReverseMap();            
            CreateMap<CreateVehicleDto, Vehicle>().ReverseMap();           
            CreateMap<UpdateVehicleDto, Vehicle>().ReverseMap();

   
        }
    }

}
