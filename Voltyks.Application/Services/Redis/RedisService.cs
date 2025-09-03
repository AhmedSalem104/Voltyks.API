using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Voltyks.Application.Interfaces.Redis;

namespace Voltyks.Application.Services.Redis
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }
        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
        }
        public async Task<string?> GetAsync(string key)
        {
            var db = _redis.GetDatabase();
            var result = await db.StringGetAsync(key);
            return result.IsNullOrEmpty ? null : result.ToString();
        }
        public async Task RemoveAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        public async Task<long> IncrementAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.StringIncrementAsync(key);
        }
        public async Task ExpireAsync(string key, TimeSpan ttl)
        {
            var db = _redis.GetDatabase();
            await db.KeyExpireAsync(key, ttl);
        }
    }

}
