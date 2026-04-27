using System.Collections.Generic;
using System.Globalization;
using Voltyks.Core.Localization;

namespace Voltyks.Application.Services.Notifications
{
    /// <summary>
    /// Bridge between a string template Key and the corresponding helper in
    /// <see cref="NotificationMessages"/>. Used by the resolver as the
    /// last-resort fallback when no DB row is present and to seed the
    /// NotificationTemplates table.
    /// </summary>
    public static class HardcodedTemplateRegistry
    {
        public delegate (string Title, string Body) Renderer(
            string lang,
            IDictionary<string, string>? parameters);

        public sealed class Entry
        {
            public string[] RequiredParams { get; init; } = System.Array.Empty<string>();
            public Renderer Render { get; init; } = default!;
            public string SampleEnTitle { get; init; } = "";
            public string SampleEnBody { get; init; } = "";
            public string SampleArTitle { get; init; } = "";
            public string SampleArBody { get; init; } = "";
        }

        private static string Get(IDictionary<string, string>? p, string key)
            => p != null && p.TryGetValue(key, out var v) ? v : "";

        private static decimal? GetDecimal(IDictionary<string, string>? p, string key)
            => p != null && p.TryGetValue(key, out var v) && decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d : (decimal?)null;

        private static double GetDouble(IDictionary<string, string>? p, string key)
            => p != null && p.TryGetValue(key, out var v) && double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d : 0;

        private static int GetInt(IDictionary<string, string>? p, string key)
            => p != null && p.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : 0;

        // ──────────────────────────────────────────────────────────────────
        // Map: template Key → (params, renderer, samples for seeding)
        // ──────────────────────────────────────────────────────────────────
        public static readonly IReadOnlyDictionary<string, Entry> All = new Dictionary<string, Entry>
        {
            ["VehicleAdditionAccepted"] = new()
            {
                Render = (lang, _) => NotificationMessages.VehicleAdditionAccepted(lang),
                SampleEnTitle = "Request Accepted",
                SampleEnBody = "Your vehicle is now available! You can add your vehicle now with us",
                SampleArTitle = "تم قبول الطلب",
                SampleArBody = "سيارتك متاحة الآن! يمكنك إضافة سيارتك معنا"
            },
            ["VehicleAdditionDeclined"] = new()
            {
                Render = (lang, _) => NotificationMessages.VehicleAdditionDeclined(lang),
                SampleEnTitle = "Request Declined",
                SampleEnBody = "The vehicle you requested already exists. Please check again",
                SampleArTitle = "تم رفض الطلب",
                SampleArBody = "السيارة المطلوبة موجودة بالفعل. يرجى البحث مرة أخرى"
            },
            ["VehicleOwnerRequestCharger"] = new()
            {
                Render = (lang, _) => NotificationMessages.VehicleOwnerRequestCharger(lang),
                SampleEnTitle = "New Charging Request 🚗",
                SampleEnBody = "Driver requested to charge at your station.",
                SampleArTitle = "طلب شحن جديد 🚗",
                SampleArBody = "طلب سائق الشحن في محطتك."
            },
            ["ChargerOwnerAccepted"] = new()
            {
                RequiredParams = new[] { "stationOwnerName" },
                Render = (lang, p) => NotificationMessages.ChargerOwnerAccepted(lang, Get(p, "stationOwnerName")),
                SampleEnTitle = "Charging Request Accepted",
                SampleEnBody = "Your request to charge at {stationOwnerName}'s station has been accepted.",
                SampleArTitle = "تم قبول طلب الشحن",
                SampleArBody = "تم قبول طلبك للشحن في محطة {stationOwnerName}."
            },
            ["ChargerOwnerRejected"] = new()
            {
                RequiredParams = new[] { "stationOwnerName" },
                Render = (lang, p) => NotificationMessages.ChargerOwnerRejected(lang, Get(p, "stationOwnerName")),
                SampleEnTitle = "Charging Request Rejected ❌",
                SampleEnBody = "Your request to charge at {stationOwnerName}'s station was rejected.",
                SampleArTitle = "تم رفض طلب الشحن ❌",
                SampleArBody = "تم رفض طلبك للشحن في محطة {stationOwnerName}."
            },
            ["ChargerOwnerConfirmed"] = new()
            {
                RequiredParams = new[] { "stationOwnerName" },
                Render = (lang, p) => NotificationMessages.ChargerOwnerConfirmed(lang, Get(p, "stationOwnerName")),
                SampleEnTitle = "Charging Request Confirmed ✅",
                SampleEnBody = "The charger {stationOwnerName} confirmed the charging session for your vehicle.",
                SampleArTitle = "تم تأكيد طلب الشحن ✅",
                SampleArBody = "أكد صاحب الشاحن {stationOwnerName} جلسة الشحن لسيارتك."
            },
            ["ChargerOwnerAborted"] = new()
            {
                Render = (lang, _) => NotificationMessages.ChargerOwnerAborted(lang),
                SampleEnTitle = "Charging session aborted",
                SampleEnBody = "The station owner aborted your charging request.",
                SampleArTitle = "تم إلغاء جلسة الشحن",
                SampleArBody = "قام صاحب المحطة بإلغاء طلب الشحن الخاص بك."
            },
            ["VehicleOwnerAbortedAfterPayment"] = new()
            {
                RequiredParams = new[] { "driverName" },
                Render = (lang, p) => NotificationMessages.VehicleOwnerAbortedAfterPayment(lang, Get(p, "driverName")),
                SampleEnTitle = "Request Aborted ❌",
                SampleEnBody = "The driver {driverName} aborted the charging session at your station after payment.",
                SampleArTitle = "تم إلغاء الطلب ❌",
                SampleArBody = "قام السائق {driverName} بإلغاء جلسة الشحن في محطتك بعد الدفع."
            },
            ["ProcessConfirmationPending"] = new()
            {
                RequiredParams = new[] { "amountCharged", "amountPaid" },
                Render = (lang, p) => NotificationMessages.ProcessConfirmationPending(
                    lang, GetDecimal(p, "amountCharged"), GetDecimal(p, "amountPaid")),
                SampleEnTitle = "Process confirmation pending",
                SampleEnBody = "Amount Charged: {amountCharged} | Amount Paid: {amountPaid}",
                SampleArTitle = "في انتظار تأكيد العملية",
                SampleArBody = "المبلغ المحصل: {amountCharged} | المبلغ المدفوع: {amountPaid}"
            },
            ["VehicleOwnerUpdateProcess"] = new()
            {
                Render = (lang, _) => NotificationMessages.VehicleOwnerUpdateProcess(lang),
                SampleEnTitle = "Process updated",
                SampleEnBody = "The vehicle owner updated process details.",
                SampleArTitle = "تم تحديث العملية",
                SampleArBody = "قام صاحب السيارة بتحديث تفاصيل العملية."
            },
            ["VehicleOwnerUpdateProcessWithFields"] = new()
            {
                RequiredParams = new[] { "fieldsCsv" },
                Render = (lang, p) => NotificationMessages.VehicleOwnerUpdateProcessWithFields(lang, Get(p, "fieldsCsv")),
                SampleEnTitle = "Process updated",
                SampleEnBody = "Updated fields → {fieldsCsv}",
                SampleArTitle = "تم تحديث العملية",
                SampleArBody = "الحقول المحدثة → {fieldsCsv}"
            },
            ["SubmitRating"] = new()
            {
                RequiredParams = new[] { "rating", "processId" },
                Render = (lang, p) => NotificationMessages.SubmitRating(lang, GetDouble(p, "rating"), GetInt(p, "processId")),
                SampleEnTitle = "New rating received ⭐",
                SampleEnBody = "You received a {rating}★ rating for process #{processId}.",
                SampleArTitle = "تم استلام تقييم جديد ⭐",
                SampleArBody = "حصلت على تقييم {rating}★ للعملية رقم #{processId}."
            },
            ["ProcessTerminated"] = new()
            {
                Render = (lang, _) => NotificationMessages.ProcessTerminated(lang),
                SampleEnTitle = "Process Terminated",
                SampleEnBody = "The process has been terminated.",
                SampleArTitle = "تم إنهاء العملية",
                SampleArBody = "تم إنهاء العملية."
            },
            ["ProcessExpired"] = new()
            {
                Render = (lang, _) => NotificationMessages.ProcessExpired(lang),
                SampleEnTitle = "Process Terminated",
                SampleEnBody = "The request has expired.",
                SampleArTitle = "تم إنهاء العملية",
                SampleArBody = "انتهت صلاحية الطلب."
            },
            ["ProcessConfirmedByCharger"] = new()
            {
                Render = (lang, _) => NotificationMessages.ProcessConfirmedByCharger(lang),
                SampleEnTitle = "Process confirmed",
                SampleEnBody = "Charger owner confirmed your session. Please submit your rating.",
                SampleArTitle = "تم تأكيد العملية",
                SampleArBody = "أكد صاحب الشاحن جلستك. يرجى إرسال التقييم."
            },
            ["ProcessConfirmedByVehicle"] = new()
            {
                Render = (lang, _) => NotificationMessages.ProcessConfirmedByVehicle(lang),
                SampleEnTitle = "Process confirmed",
                SampleEnBody = "Vehicle owner confirmed the session completion.",
                SampleArTitle = "تم تأكيد العملية",
                SampleArBody = "أكد صاحب السيارة اكتمال الجلسة."
            },
            ["ProcessStarted"] = new()
            {
                RequiredParams = new[] { "whoStarted" },
                Render = (lang, p) => NotificationMessages.ProcessStarted(lang, Get(p, "whoStarted")),
                SampleEnTitle = "Process started",
                SampleEnBody = "{whoStarted} started the process.",
                SampleArTitle = "بدأت العملية",
                SampleArBody = "{whoStarted} بدأ العملية."
            },
            ["DefaultRatingApplied"] = new()
            {
                RequiredParams = new[] { "rating", "processId" },
                Render = (lang, p) => NotificationMessages.DefaultRatingApplied(lang, GetDouble(p, "rating"), GetInt(p, "processId")),
                SampleEnTitle = "Default rating applied",
                SampleEnBody = "A default {rating}★ rating was applied for process #{processId}.",
                SampleArTitle = "تم تطبيق التقييم الافتراضي",
                SampleArBody = "تم تطبيق تقييم افتراضي {rating}★ للعملية رقم #{processId}."
            },
            ["ReportFiled"] = new()
            {
                RequiredParams = new[] { "reporterName" },
                Render = (lang, p) => NotificationMessages.ReportFiled(lang, Get(p, "reporterName")),
                SampleEnTitle = "{reporterName} filed a report against you",
                SampleEnBody = "Open the process to review the report details.",
                SampleArTitle = "قام {reporterName} بالإبلاغ عنك",
                SampleArBody = "افتح العملية لمراجعة تفاصيل البلاغ."
            }
        };
    }
}
