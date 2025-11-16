using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public record NotificationResultDto(
     int NotificationId,
     int RequestId,
     string RecipientUserId,
     string Title,
     string Body,
     string NotificationType,
     DateTime SentAt,
     int PushSentCount,

     // القيم الجديدة (اختيارية)
     int? ProcessId = null,
     decimal? EstimatedPrice = null,
     decimal? AmountCharged = null,
     decimal? AmountPaid = null,

     object? ExtraData = null
 );


}
