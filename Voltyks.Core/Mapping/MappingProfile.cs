using AutoMapper;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.BrandsDTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Core.DTOs.FeesConfig;
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



            // ChargingRequestEntity -> ChargingRequestDetailsDto
            CreateMap<ChargingRequest, ChargingRequestDetailsDto>()
                .ForMember(d => d.RequestId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.RequestedAt, o => o.MapFrom(s => s.RequestedAt))
                .ForMember(d => d.CarOwnerId, o => o.MapFrom(s => s.CarOwner.Id))
                .ForMember(d => d.CarOwnerName, o => o.MapFrom(s => $"{s.CarOwner.FirstName} {s.CarOwner.LastName}"))
                .ForMember(d => d.StationOwnerId, o => o.MapFrom(s => s.Charger.User.Id))
                .ForMember(d => d.StationOwnerName, o => o.MapFrom(s => $"{s.Charger.User.FirstName} {s.Charger.User.LastName}"))
                .ForMember(d => d.ChargerId, o => o.MapFrom(s => s.ChargerId))
                .ForMember(d => d.Protocol, o => o.MapFrom(s => s.Charger.Protocol != null ? s.Charger.Protocol.Name : "Unknown"))
                .ForMember(d => d.CapacityKw, o => o.MapFrom(s => s.Charger.Capacity != null ? s.Charger.Capacity.kw : 0))
                .ForMember(d => d.PricePerHour, o => o.MapFrom(s => s.Charger.PriceOption != null ? $"{s.Charger.PriceOption.Value} EGP" : "N/A"))
                .ForMember(d => d.AdapterAvailability, o => o.MapFrom(s => s.Charger.Adaptor == true ? "Available" : "Not Available"))
                .ForMember(d => d.ChargerArea, o => o.MapFrom(s => s.Charger.Address != null ? s.Charger.Address.Area : "N/A"))
                .ForMember(d => d.ChargerStreet, o => o.MapFrom(s => s.Charger.Address != null ? s.Charger.Address.Street : "N/A"))
                // تجاهل الحقول اللي هتتحسب في السيرفس
                .ForMember(d => d.VehicleBrand, o => o.Ignore())
                .ForMember(d => d.VehicleModel, o => o.Ignore())
                .ForMember(d => d.VehicleColor, o => o.Ignore())
                .ForMember(d => d.VehiclePlate, o => o.Ignore())
                .ForMember(d => d.VehicleCapacity, o => o.Ignore())
                .ForMember(d => d.VehicleArea, o => o.Ignore())
                .ForMember(d => d.VehicleStreet, o => o.Ignore())
                .ForMember(d => d.EstimatedArrival, o => o.Ignore())
                .ForMember(d => d.EstimatedPrice, o => o.Ignore())
                .ForMember(d => d.DistanceInKm, o => o.Ignore());


          
            // من Entity لعرض النتيجة
            CreateMap<FeesConfig, FeesConfigDto>();

            // من DTO لتحديث القيم للـ Entity
            CreateMap<FeesConfigUpdateDto, FeesConfig>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        }
    }


}
