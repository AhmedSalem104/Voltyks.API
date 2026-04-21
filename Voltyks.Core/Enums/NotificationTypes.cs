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
        public const string ChargerOwner_ConfirmedProcessSuccessfully = "ChargerOwner_ConfirmedProcessSuccessfully";
        public const string VehicleOwner_ProcessAbortedAfterPaymentSuccessfully = "VehicleOwner_ProcessAbortedAfterPaymentSuccessfully";
        public const string VehicleOwner_CreateProcess = "VehicleOwner_CreateProcess";
        public const string VehicleOwner_UpdateProcess = "VehicleOwner_UpdateProcess";
        public const string Report_ChargerOwnerToVehicleOwner = "Report_ChargerOwnerToVehicleOwner";
        public const string Report_VehicleOwnerToChargerOwner = "Report_VehicleOwnerToChargerOwner";

        // Process lifecycle
        public const string Process_Started = "Process_Started";
        public const string ChargerOwner_ConfirmProcess = "ChargerOwner_ConfirmProcess";
        public const string VehicleOwner_ConfirmProcess = "VehicleOwner_ConfirmProcess";
        public const string ChargerOwner_ProcessAborted = "ChargerOwner_ProcessAborted";

        // Process Termination (unified notification for all termination paths)
        public const string Process_Terminated = "Process_Terminated";

        // Admin Notifications
        public const string Admin_Report_Created = "Admin_Report_Created";
        public const string Admin_Complaint_Created = "Admin_Complaint_Created";
        public const string Admin_Reservation_Created = "Admin_Reservation_Created";

        // Vehicle Addition Requests
        public const string VehicleAdditionRequest_Accepted = "VehicleAdditionRequest_Accepted";
        public const string VehicleAdditionRequest_Declined = "VehicleAdditionRequest_Declined";
    }
}
