using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Voltyks.Persistence.Entities
{
    public static class ErrorMessages
    {
        public const string PhoneRequired = "PhoneRequired";
        public const string EmailRequired = "EmailRequired";
        public const string PhoneAlreadyExists = "PhoneAlreadyExists";
        public const string PhoneIsaVailable = "PhoneIsaVailable";
        public const string EmailIsaVailable = "EmailIsaVailable";
        public const string EmailAlreadyExists = "EmailAlreadyExists";
        public const string ValidationFailed = "ValidationFailed";
        public const string TokenExpired = "TokenExpired";
        public const string UnauthorizedAccess = "UnauthorizedAccess";
        public const string GeneralError = "GeneralError";
        public const string PhoneNumberNotExist = "PhoneNumberNotExist";
        public const string ExceededMaximumOTPAttempts = "ExceededMaximumOTPAttempts";
        public const string OTPSendingFailed = "OTPSendingFailed";
        public const string otpSentSuccessfully = "otpSentSuccessfully";
        public const string otpCodeExpiredOrNotFound = "otpCodeExpiredOrNotFound";
        public const string otpCodeInvalid = "otpCodeInvalid";
        public const string otpCodeNotVerifiedOrExpired = "otpCodeNotVerifiedOrExpired";
        public const string userNotFound = "userNotFound";
        public const string errorRemovingOldPassword = "errorRemovingOldPassword";
        public const string errorSettingNewPassword = "errorSettingNewPassword";
        public const string invalidOtp = "invalidOtp";
        public const string otpAttemptLimitExceededTryLater = "otpAttemptLimitExceededTryLater";
        public const string otpAttemptsExceededBlockedForMinutes = "otpAttemptsExceededBlockedForMinutes";
        public const string failedToSendOtp = "failedToSendOtp";

        public const string phoneAlreadyInUse = "phoneAlreadyInUse";
        public const string emailAlreadyInUse = "emailAlreadyInUse";
        public const string invalidPhoneFormat = "invalidPhoneFormat";
        public const string invalidEmailFormat = "invalidEmailFormat";
        public const string UserCreationFaild = "UserCreationFaild";


        

        public const string invalidPasswordOrEmailAddress = "invalidPasswordOrEmailAddress";
        public const string emailDoesNotExist = "emailDoesNotExist";
        public const string noPhoneAssociated = "noPhoneAssociated";
        public const string invalidExternalAuthentication = "invalidExternalAuthentication";
        public const string invalidRefreshToken = "invalidRefreshToken";
        public const string refreshTokenMismatch = "refreshTokenMismatch";
        public const string invalidExternalToken = "invalidExternalToken";
        public const string unsupportedProvider = "unsupportedProvider";
        public const string invalidOrMismatchedToken = "invalidOrMismatchedToken";
        public const string invalidPhoneNumber = "invalidPhoneNumber";






    }
}
