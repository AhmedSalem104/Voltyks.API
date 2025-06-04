using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs
{
    public class ApiResponse<T>
    {
        public bool Status { get; set; } = true;
        public string Message { get; set; }
        public T Data { get; set; }


        // أضفنا خاصية قائمة الأخطاء
        public List<string>? Errors { get; set; }

        public ApiResponse() { }

        public ApiResponse(T data, string message = null, bool status = false, List<string>? errors = null)
        {
            Data = data;
            Message = message ?? "Success";
            Errors = errors;
        }

        public ApiResponse(string message, bool status = false, List<string>? errors = null)
        {
            Status = status;
            Message = message;
            Errors = errors;
        }
    }
}



