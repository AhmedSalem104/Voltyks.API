using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces
{
    public interface IRedisService
    {
        Task SetAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetAsync(string key);
        Task RemoveAsync(string key);
    }

}
