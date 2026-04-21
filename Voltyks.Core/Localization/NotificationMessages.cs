using Voltyks.Core.Constants;

namespace Voltyks.Core.Localization
{
    /// <summary>
    /// Central registry for user-facing notification texts in all supported languages.
    /// Every helper returns a (Title, Body) tuple. Pass a normalized language string
    /// (use <see cref="Languages.Normalize"/> on the caller's input).
    /// </summary>
    public static class NotificationMessages
    {
        // ============ Vehicle Addition Requests ============

        public static (string Title, string Body) VehicleAdditionAccepted(string lang) =>
            lang == Languages.Ar
                ? ("تم قبول الطلب", "سيارتك متاحة الآن! يمكنك إضافة سيارتك معنا")
                : ("Request Accepted", "Your vehicle is now available! You can add your vehicle now with us");

        public static (string Title, string Body) VehicleAdditionDeclined(string lang) =>
            lang == Languages.Ar
                ? ("تم رفض الطلب", "السيارة المطلوبة موجودة بالفعل. يرجى البحث مرة أخرى")
                : ("Request Declined", "The vehicle you requested already exists. Please check again");

        // ============ Charging Request Lifecycle ============

        public static (string Title, string Body) VehicleOwnerRequestCharger(string lang) =>
            lang == Languages.Ar
                ? ("طلب شحن جديد 🚗", "طلب سائق الشحن في محطتك.")
                : ("New Charging Request 🚗", "Driver requested to charge at your station.");

        public static (string Title, string Body) ChargerOwnerAccepted(string lang, string stationOwnerName) =>
            lang == Languages.Ar
                ? ("تم قبول طلب الشحن", $"تم قبول طلبك للشحن في محطة {stationOwnerName}.")
                : ("Charging Request Accepted", $"Your request to charge at {stationOwnerName}'s station has been accepted.");

        public static (string Title, string Body) ChargerOwnerRejected(string lang, string stationOwnerName) =>
            lang == Languages.Ar
                ? ("تم رفض طلب الشحن ❌", $"تم رفض طلبك للشحن في محطة {stationOwnerName}.")
                : ("Charging Request Rejected ❌", $"Your request to charge at {stationOwnerName}'s station was rejected.");

        public static (string Title, string Body) ChargerOwnerConfirmed(string lang, string stationOwnerName) =>
            lang == Languages.Ar
                ? ("تم تأكيد طلب الشحن ✅", $"أكد صاحب الشاحن {stationOwnerName} جلسة الشحن لسيارتك.")
                : ("Charging Request Confirmed ✅", $"The charger {stationOwnerName} confirmed the charging session for your vehicle.");

        public static (string Title, string Body) ChargerOwnerAborted(string lang) =>
            lang == Languages.Ar
                ? ("تم إلغاء جلسة الشحن", "قام صاحب المحطة بإلغاء طلب الشحن الخاص بك.")
                : ("Charging session aborted", "The station owner aborted your charging request.");

        public static (string Title, string Body) VehicleOwnerAbortedAfterPayment(string lang, string driverName) =>
            lang == Languages.Ar
                ? ("تم إلغاء الطلب ❌", $"قام السائق {driverName} بإلغاء جلسة الشحن في محطتك بعد الدفع.")
                : ("Request Aborted ❌", $"The driver {driverName} aborted the charging session at your station after payment.");

        // ============ Process Lifecycle ============

        public static (string Title, string Body) ProcessConfirmationPending(string lang, decimal? amountCharged, decimal? amountPaid) =>
            lang == Languages.Ar
                ? ("في انتظار تأكيد العملية", $"المبلغ المحصل: {(amountCharged ?? 0m):0.##} | المبلغ المدفوع: {(amountPaid ?? 0m):0.##}")
                : ("Process confirmation pending", $"Amount Charged: {(amountCharged ?? 0m):0.##} | Amount Paid: {(amountPaid ?? 0m):0.##}");

        public static (string Title, string Body) VehicleOwnerUpdateProcess(string lang) =>
            lang == Languages.Ar
                ? ("تم تحديث العملية", "قام صاحب السيارة بتحديث تفاصيل العملية.")
                : ("Process updated", "The vehicle owner updated process details.");

        public static (string Title, string Body) VehicleOwnerUpdateProcessWithFields(string lang, string fieldsCsv) =>
            lang == Languages.Ar
                ? ("تم تحديث العملية", $"الحقول المحدثة → {fieldsCsv}")
                : ("Process updated", $"Updated fields → {fieldsCsv}");

        public static (string Title, string Body) SubmitRating(string lang, double rating, int processId) =>
            lang == Languages.Ar
                ? ("تم استلام تقييم جديد ⭐", $"حصلت على تقييم {rating:0.#}★ للعملية رقم #{processId}.")
                : ("New rating received ⭐", $"You received a {rating:0.#}★ rating for process #{processId}.");

        public static (string Title, string Body) ProcessTerminated(string lang) =>
            lang == Languages.Ar
                ? ("تم إنهاء العملية", "تم إنهاء العملية.")
                : ("Process Terminated", "The process has been terminated.");

        public static (string Title, string Body) ProcessExpired(string lang) =>
            lang == Languages.Ar
                ? ("تم إنهاء العملية", "انتهت صلاحية الطلب.")
                : ("Process Terminated", "The request has expired.");

        public static (string Title, string Body) ProcessConfirmedByCharger(string lang) =>
            lang == Languages.Ar
                ? ("تم تأكيد العملية", "أكد صاحب الشاحن جلستك. يرجى إرسال التقييم.")
                : ("Process confirmed", "Charger owner confirmed your session. Please submit your rating.");

        public static (string Title, string Body) ProcessConfirmedByVehicle(string lang) =>
            lang == Languages.Ar
                ? ("تم تأكيد العملية", "أكد صاحب السيارة اكتمال الجلسة.")
                : ("Process confirmed", "Vehicle owner confirmed the session completion.");

        public static (string Title, string Body) ProcessStarted(string lang, string whoStarted) =>
            lang == Languages.Ar
                ? ("بدأت العملية", $"{whoStarted} بدأ العملية.")
                : ("Process started", $"{whoStarted} started the process.");

        public static (string Title, string Body) DefaultRatingApplied(string lang, double rating, int processId) =>
            lang == Languages.Ar
                ? ("تم تطبيق التقييم الافتراضي", $"تم تطبيق تقييم افتراضي {rating:0.#}★ للعملية رقم #{processId}.")
                : ("Default rating applied", $"A default {rating:0.#}★ rating was applied for process #{processId}.");

        // ============ User Reports ============

        public static (string Title, string Body) ReportFiled(string lang, string reporterName) =>
            lang == Languages.Ar
                ? ($"قام {reporterName} بالإبلاغ عنك", "افتح العملية لمراجعة تفاصيل البلاغ.")
                : ($"{reporterName} filed a report against you", "Open the process to review the report details.");
    }
}
