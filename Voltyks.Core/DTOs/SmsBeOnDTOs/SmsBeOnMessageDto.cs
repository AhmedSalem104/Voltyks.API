using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.SmsBeOnDTOs
{
    public class SmsBeOnMessageDto
    {
        public string Sender { get; set; } = string.Empty;    // اسم المرسل (SenderName)
        public string Mobile { get; set; } = string.Empty;    // رقم الموبايل المستلم
        public string Message { get; set; } = string.Empty;   // نص الرسالة
        public string Name { get; set; }       // اسم الرسالة أو النموذج
        public string Type { get; set; }       // نوع الرسالة: "sms"
        public string Otp_length { get; set; } // عدد أرقام الكود (مثلاً "4")
        public string Lang { get; set; }
    }

}
