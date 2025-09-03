using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public class PaymobTransactionResponse
    {
        public int Id { get; set; }
        public bool Pending { get; set; }
        public bool Success { get; set; }
        public int AmountCents { get; set; }
        public bool IsAuth { get; set; }
        public bool IsCapture { get; set; }
        public bool IsVoided { get; set; }
        public bool IsRefunded { get; set; }
        public bool Is3dSecure { get; set; }
        public int IntegrationId { get; set; }
        public int ProfileId { get; set; }
        public int Order { get; set; } // ده بيرجع الـ OrderId
        public DateTime CreatedAt { get; set; }
    }

}
