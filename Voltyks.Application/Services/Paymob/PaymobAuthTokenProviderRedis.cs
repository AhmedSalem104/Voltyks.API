using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Core.DTOs.Paymob.Options;


namespace Voltyks.Application.Services.Paymob
{

    public sealed class PaymobAuthTokenProviderRedis : IPaymobAuthTokenProvider
    {
        private readonly IRedisService _redis;
        private readonly IHttpClientFactory _httpFactory;
        private readonly PaymobOptions _opt;
        private readonly ILogger<PaymobAuthTokenProviderRedis> _log;
        private const string TokenKey = "paymob:auth_token:v1";
        private const string LockKey = "paymob:auth_token:lock";
        private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(50);
        private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(15);

        public PaymobAuthTokenProviderRedis(
            IRedisService redis,
            IHttpClientFactory httpFactory,
            IOptions<PaymobOptions> opt,
            ILogger<PaymobAuthTokenProviderRedis> log)
        {
            _redis = redis;
            _httpFactory = httpFactory;
            _opt = opt.Value;
            _log = log;
        }

        public async Task<string> GetAsync(CancellationToken ct = default)
        {
            // 1) لو متخزن خلاص في الكاش
            var cached = await _redis.GetAsync(TokenKey);
            if (!string.IsNullOrWhiteSpace(cached))
                return cached!;

            // 2) حاول تاخد "لوك" (بسيط) عشان تمنع تنافس الطلبات
            var lockCount = await _redis.IncrementAsync(LockKey);
            if (lockCount == 1)
            {
                // احجز TTL للّوك (failsafe)
                await _redis.ExpireAsync(LockKey, LockTtl);
                try
                {
                    // Double-check بعد ما خدت اللّوك
                    cached = await _redis.GetAsync(TokenKey);
                    if (!string.IsNullOrWhiteSpace(cached))
                        return cached!;

                    // 3) اطلب التوكن من Paymob
                    var http = _httpFactory.CreateClient("paymob");
                    var res = await http.PostAsJsonAsync($"{_opt.ApiBase}/auth/tokens", new PaymobAuthReq(_opt.ApiKey), ct);
                    res.EnsureSuccessStatusCode();
                    var data = await res.Content.ReadFromJsonAsync<PaymobAuthRes>(cancellationToken: ct);
                    var token = data!.token;

                    // 4) خزّنه في Redis بمدة صلاحية
                    await _redis.SetAsync(TokenKey, token, TokenTtl);

                    return token;
                }
                finally
                {
                    // حرّر اللّوك
                    await _redis.RemoveAsync(LockKey);
                }
            }
            else
            {
                // 3b) حد تاني واخد اللّوك: استنى شوية وجرّب تقراه من الكاش
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(300, ct);
                    cached = await _redis.GetAsync(TokenKey);
                    if (!string.IsNullOrWhiteSpace(cached))
                        return cached!;
                }

                // fallback نادر: لو لسه مش متاح، اعمل طلب مباشر (من غير لوك)
                var http = _httpFactory.CreateClient("paymob");
                var res = await http.PostAsJsonAsync($"{_opt.ApiBase}/auth/tokens", new PaymobAuthReq(_opt.ApiKey), ct);
                res.EnsureSuccessStatusCode();
                var data = await res.Content.ReadFromJsonAsync<PaymobAuthRes>(cancellationToken: ct);
                var token = data!.token;

                await _redis.SetAsync(TokenKey, token, TokenTtl);
                return token;
            }
        }

        public async Task InvalidateAsync()
        {
            await _redis.RemoveAsync(TokenKey);
        }
    }
}
