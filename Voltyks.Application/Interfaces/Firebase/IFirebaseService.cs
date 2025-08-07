using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Firebase
{
    public interface IFirebaseService
    {
        Task SendNotificationAsync(string deviceToken, string title, string body, int chargingRequestID);
    }

}
