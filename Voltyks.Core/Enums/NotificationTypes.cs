using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.Enums
{
    public static class NotificationTypes
    {
        public const string VehicleOwner_RequestCharger = "VehicleOwner_RequestCharger";
        public const string ChargerOwner_AcceptRequest = "ChargerOwner_AcceptRequest";
        public const string ChargerOwner_RejectRequest = "ChargerOwner_RejectRequest";
        public const string VehicleOwner_CompleteProcessSuccessfully = "VehicleOwner_CompleteProcessSuccessfully";
        public const string VehicleOwner_ProcessAbortedAfterPaymentSuccessfully = "VehicleOwner_ProcessAbortedAfterPaymentSuccessfully";
    }
}
