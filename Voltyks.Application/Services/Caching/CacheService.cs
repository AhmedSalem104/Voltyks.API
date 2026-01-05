using System.Text.Json;
using Voltyks.Application.Interfaces.Caching;
using Voltyks.Application.Interfaces.Redis;

namespace Voltyks.Application.Services.Caching
{
    public class CacheService : ICacheService
    {
        private readonly IRedisService _redisService;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public CacheService(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            var json = await _redisService.GetAsync(key);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _redisService.SetAsync(key, json, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await _redisService.RemoveAsync(key);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached is not null)
                return cached;

            var value = await factory();
            await SetAsync(key, value, expiry);
            return value;
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            var keys = await _redisService.GetAllKeysAsync(pattern);
            var keyList = keys.ToList();

            if (keyList.Count == 0) return;

            // Parallel removal for better performance
            await Task.WhenAll(keyList.Select(key => _redisService.RemoveAsync(key)));
        }
    }
}
