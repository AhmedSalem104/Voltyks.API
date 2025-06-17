using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Voltyks.Persistence.Entities
{
    public static class ErrorMessages
    {
        public const string PhoneRequired = "PhoneRequired";
        public const string EmailRequired = "EmailRequired";
        public const string PhoneAlreadyExists = "PhoneAlreadyExists";
        public const string EmailAlreadyExists = "EmailAlreadyExists";
        public const string ValidationFailed = "ValidationFailed";
        public const string TokenExpired = "TokenExpired";
        public const string UnauthorizedAccess = "UnauthorizedAccess";
        public const string GeneralError = "GeneralError";
        public const string PhoneNumberNotExist = "PhoneNumberNotExist";
        public const string ExceededMaximumOTPAttempts = "ExceededMaximumOTPAttempts";
        public const string OTPSendingFailed = "OTPSendingFailed";
        public const string OtpSentSuccessfully = "OtpSentSuccessfully";
        public const string OtpCodeExpiredOrNotFound = "OtpCodeExpiredOrNotFound";
        public const string OtpCodeInvalid = "OtpCodeInvalid";
        public const string OtpCodeNotVerifiedOrExpired = "OtpCodeNotVerifiedOrExpired";
        public const string UserNotFound = "UserNotFound";
        public const string ErrorRemovingOldPassword = "ErrorRemovingOldPassword";
        public const string ErrorSettingNewPassword = "ErrorSettingNewPassword";
        public const string OtpAttemptLimitExceededTryLater = "OtpAttemptLimitExceededTryLater";
        public const string OtpAttemptsExceededBlockedForMinutes = "OtpAttemptsExceededBlockedForMinutes";
        public const string InvalidPhoneFormat = "InvalidPhoneFormat";
        public const string InvalidEmailFormat = "InvalidEmailFormat";
        public const string UserCreationFailed = "UserCreationFailed";
        public const string InvalidPasswordOrEmailAddress = "InvalidPasswordOrEmailAddress";
        public const string EmailDoesNotExist = "EmailDoesNotExist";
        public const string NoPhoneAssociated = "NoPhoneAssociated";
        public const string InvalidExternalAuthentication = "InvalidExternalAuthentication";
        public const string InvalidRefreshToken = "InvalidRefreshToken";
        public const string RefreshTokenMismatch = "RefreshTokenMismatch";
        public const string InvalidExternalToken = "InvalidExternalToken";
        public const string UnsupportedProvider = "UnsupportedProvider";
        public const string InvalidOrMismatchedToken = "InvalidOrMismatchedToken";
        public const string InvalidPhoneNumber = "InvalidPhoneNumber";
        public const string FailedGetBrands = "FailedGetBrands";
        public const string NoModelsFoundForThisBrand = "NoModelsFoundForThisBrand";
        public const string NoYearsFoundForThisModel = "NoYearsFoundForThisModel";
        public const string OtpLimitExceededForToday = "OtpLimitExceededForToday";
        public const string UserAlreadyHasVehicle = "UserAlreadyHasVehicle";
        public const string UserNotAuthenticated = "UserNotAuthenticated";
        public const string VehicleNotFoundOrNotAuthorized = "VehicleNotFoundOrNotAuthorized";
    }

}
