
using System.Net;
using Voltyks.Core.ErrorModels;
using Voltyks.Core.Exceptions;


namespace Voltyks.API.Middelwares
{
    public class GlobalErrorHandlingMiddelwares
    {
        // حقن delegate لطلب HTTP التالي في الـ pipeline
        private readonly RequestDelegate _next;

        // حقن الـ Logger لتسجيل الأخطاء أو الأحداث
        private readonly ILogger<GlobalErrorHandlingMiddelwares> _logger;

        // Constructor يستقبل الـ delegate والـ logger من خلال الـ dependency injection
        public GlobalErrorHandlingMiddelwares(RequestDelegate next, ILogger<GlobalErrorHandlingMiddelwares> logger)
        {
            _next = next;
            _logger = logger;
        }

        // هذه هي الدالة الأساسية التي يتم استدعاؤها أثناء تنفيذ الطلب
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // تمرير الطلب للـ middleware التالي في الـ pipeline
                await _next.Invoke(context);

                // في حال لم يتم العثور على الـ endpoint (404)
                await HandlingNotFoundEndpoint(context);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ في سجل النظام باستخدام logger
                _logger.LogError(ex, ex.Message);

                await HandlingErrorAsync(context, ex);
            }
        }

        private static async Task HandlingErrorAsync(HttpContext context, Exception ex)
        {
            // إعداد نوع المحتوى كـ JSON للرد
            context.Response.ContentType = "application/json";

            // إنشاء كائن الخطأ لتضمين الحالة والرسالة
            var response = new ErrorDetails
            {
                StatusCode = StatusCodes.Status500InternalServerError, // القيمة الافتراضية
                ErrorMessage = ex.Message // عرض رسالة الخطأ الفعلية
            };

            // تحديد كود الحالة حسب نوع الاستثناء باستخدام switch expression
            response.StatusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound, // إذا كان الخطأ من نوع NotFound
                BadRequestException => StatusCodes.Status400BadRequest, // إذا كان الخطأ من نوع BadRequest
                UnAuthorizedException => StatusCodes.Status401Unauthorized,


                _ => StatusCodes.Status500InternalServerError // لجميع الأخطاء الأخرى
            };

            // ضبط كود الحالة في الرد حسب النتيجة أعلاه
            context.Response.StatusCode = response.StatusCode;

            // إرسال الرد بصيغة JSON
            await context.Response.WriteAsJsonAsync(response);
        }

        private static async Task HandlingNotFoundEndpoint(HttpContext context)
        {
            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                // إعداد نوع المحتوى كـ JSON
                context.Response.ContentType = "application/json";

                // إنشاء كائن يحتوي على تفاصيل الخطأ
                var response = new ErrorDetails()
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = $"End Point {context.Request.Path} is Not Found" // رسالة توضح المسار المفقود
                };

                // إرسال الرد للمستخدم بصيغة JSON
                await context.Response.WriteAsJsonAsync(response);
            }
        }

        private static int HandleValidationExceptionAsync(ValidationException validationException, ErrorDetails response)
        {
           response.Errors = validationException.Errors;
            return StatusCodes.Status400BadRequest;
        }
    }

}
