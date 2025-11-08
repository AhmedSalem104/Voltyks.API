using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Redis
{
    public interface IRedisService
    {
        Task SetAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetAsync(string key);
        Task RemoveAsync(string key);
        Task<long> IncrementAsync(string key);          
        Task ExpireAsync(string key, TimeSpan ttl);       
        Task<IEnumerable<string>> GetAllKeysAsync(string pattern = "*");

    }

}
