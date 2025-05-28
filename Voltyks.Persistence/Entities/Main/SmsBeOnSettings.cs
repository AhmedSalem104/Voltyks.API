using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main
{
    public class SmsBeOnSettings
    {
        public string BaseUrl { get; set; }
        public string SenderName { get; set; }
        public string Name { get; set; }       // اسم الرسالة أو النموذج
        public string Type { get; set; }       // نوع الرسالة: "sms"
        public string Sender { get; set; }     // اسم المرسل
        public string Mobile { get; set; }     // رقم الهاتف
        public string Otp_length { get; set; } // عدد أرقام الكود (مثلاً "4")
        public string Lang { get; set; }
        public string Token { get; set; }
    }
}
