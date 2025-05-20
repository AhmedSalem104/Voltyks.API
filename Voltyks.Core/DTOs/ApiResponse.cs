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

        public ApiResponse() { }

        public ApiResponse(T data, string message = null)
        {
            Data = data;
            Message = message ?? "Success";
        }

        public ApiResponse(string message, bool status = false)
        {
            Status = status;
            Message = message;
        }
    }

}
