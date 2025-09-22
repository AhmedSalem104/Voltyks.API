
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Core.DTOs.VehicleDTOs;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class UserDetailsDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailable { get; set; } 

        public List<VehicleDto> Vehicles { get; set; }
        public List<ChargerDto> Chargers { get; set; }
        //public List<ChargingRequest> ChargingRequests { get; set; }
        //public List<ChargingRequestDetailsDto> ChargingRequests { get; set; } = new();

    }

    public class PriceOptionDto
    {
        public string OptionName { get; set; }
        public decimal Price { get; set; }
    }


}
