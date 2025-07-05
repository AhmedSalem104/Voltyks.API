using AutoMapper;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.BrandsDTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.ModelDTOs;
using Voltyks.Core.DTOs.VehicleDTOs;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using ChargerDto = Voltyks.Core.DTOs.Charger.ChargerDto;
using VehicleDto = Voltyks.Core.DTOs.VehicleDTOs.VehicleDto;

namespace Voltyks.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Brand & Model
            CreateMap<Brand, BrandDto>();
            CreateMap<Model, ModelDto>()
                .ForMember(dest => dest.ModelId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.Name));

            // Vehicle
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand.Name))
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.Model.Name))
                .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.Year));
            CreateMap<VehicleDto, Vehicle>().ReverseMap();
            CreateMap<CreateVehicleDto, Vehicle>().ReverseMap();
            CreateMap<UpdateVehicleDto, Vehicle>().ReverseMap();

            // Charger
            CreateMap<AddChargerDto, Charger>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                .ForMember(dest => dest.DateAdded, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            CreateMap<Charger, ChargerDto>()
                .ForMember(dest => dest.Protocol, opt => opt.MapFrom(src => src.Protocol.Name))
                .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.Capacity.KW))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PriceOption.Value))
                .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Address.Area))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address.Longitude));

    

            CreateMap<Capacity, CapacityDto>();
            CreateMap<Protocol, ProtocolDto>();
            CreateMap<PriceOption, PriceByCapacityDto>();
            CreateMap<PriceOption, PriceOptionDto>();

            // User
            CreateMap<AppUser, UserDetailsDto>();
        }
    }
}
