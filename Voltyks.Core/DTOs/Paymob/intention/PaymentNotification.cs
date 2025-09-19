using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.intention
{
    public class PaymentNotification
    {
        public string RawBody { get; set; } // يمثل الجسم الخام للإشعار من Paymob

        // يمكن إضافة أي خصائص أخرى تحتاجها بناءً على البيانات التي تستقبلها من Paymob
        public string MerchantOrderId { get; set; }
        public long PaymobOrderId { get; set; }
        public string Status { get; set; } // الحالة (مثلاً: "Paid", "Failed", "Pending")
        public long AmountCents { get; set; }
        // إضافة المزيد من الحقول هنا إذا كنت بحاجة إليها
    }

}
