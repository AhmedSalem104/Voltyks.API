using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Firebase
{
    public interface IFirebaseService
    {
        Task SendNotificationAsync(string deviceToken, string title, string body, int chargingRequestID , string NotificationType, Dictionary<string, string>? extraData = null);
     
   
    }

}
