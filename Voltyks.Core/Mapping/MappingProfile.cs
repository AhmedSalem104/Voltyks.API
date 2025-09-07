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


            // ===== Simple Mappings =====
            CreateMap<Capacity, CapacityDto>().ReverseMap();
            CreateMap<Protocol, ProtocolDto>();
            CreateMap<PriceOption, PriceByCapacityDto>();
            CreateMap<PriceOption, PriceOptionDto>();
            CreateMap<AppUser, UserDetailsDto>();


            // ===== Brand & Model =====
            CreateMap<Brand, BrandDto>();

            CreateMap<Model, ModelDto>()
                .ForMember(dest => dest.ModelId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.Name));

            CreateMap<Vehicle, VehicleDto>()
                   .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
                   .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.Model != null ? src.Model.Name : null))
                   .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.Model != null ? src.Model.Capacity : 0))
                   .ReverseMap()
               .ForPath(dest => dest.Brand, opt => opt.Ignore())
               .ForPath(dest => dest.Model, opt => opt.Ignore());



            CreateMap<CreateAndUpdateVehicleDto, Vehicle>().ReverseMap();
            CreateMap<UpdateVehicleDto, Vehicle>().ReverseMap();

            // ===== Charger Add =====
            CreateMap<AddChargerDto, Charger>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                .ForMember(dest => dest.DateAdded, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());


            // ===== Charger -> ChargerDto =====
            CreateMap<Charger, ChargerDto>()
                .ForMember(dest => dest.Protocol, opt => opt.MapFrom(src => src.Protocol != null ? src.Protocol.Name : null))
                .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.Capacity != null ? src.Capacity.kw : 0))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PriceOption != null ? src.PriceOption.Value : 0))
                .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Address != null ? src.Address.Area : null))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street : null))
                .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address != null ? src.Address.BuildingNumber : null))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Latitude : 0))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Longitude : 0))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.DateAdded, opt => opt.MapFrom(src => src.DateAdded))
                .ForSourceMember(src => src.IsDeleted, opt => opt.DoNotValidate());

            // ===== Charger -> ChargerDetailsDto =====
            CreateMap<Charger, ChargerDetailsDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.AverageRating))
                .ForMember(dest => dest.RatingCount, opt => opt.MapFrom(src => src.RatingCount))
                .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Address.Area))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.Protocol, opt => opt.MapFrom(src => src.Protocol.Name))
                .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => $"{src.Capacity.kw} KW/h"))
                .ForMember(dest => dest.PricePerHour, opt => opt.MapFrom(src => $"{src.PriceOption.Value}/1Hr"))
                .ForMember(dest => dest.AdapterAvailability, opt => opt.MapFrom(src => (bool)src.Adaptor ? "Available" : "Not Available"))
                .ForMember(dest => dest.PriceEstimated, opt => opt.Ignore())
                .ForMember(dest => dest.DistanceInKm, opt => opt.Ignore())
                .ForMember(dest => dest.EstimatedArrival, opt => opt.Ignore());

            // ===== Charger -> NearChargerDto =====
            CreateMap<Charger, NearChargerDto>()
                .ForMember(dest => dest.ChargerId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.Capacity.kw))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PriceOption.Value))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address.Longitude))
                .ForMember(dest => dest.DistanceInKm, opt => opt.Ignore()); // نحسبها يدوي
        }
    }


}
