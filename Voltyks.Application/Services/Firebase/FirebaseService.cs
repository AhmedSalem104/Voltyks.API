using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Redis;

namespace Voltyks.Application.Services.Firebase
{
    public class FirebaseService : IFirebaseService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _projectId;
        private readonly ILogger<FirebaseService> _logger;
        private readonly string _serviceAccountRelativePath;
        private readonly IRedisService _redisService;
        private GoogleCredential? _cachedCredential;
        private readonly SemaphoreSlim _credentialLock = new SemaphoreSlim(1, 1);



        public FirebaseService(IConfiguration config, ILogger<FirebaseService> logger, IRedisService redisService, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _logger = logger;
            _serviceAccountRelativePath = _config["Firebase:ServiceAccountFile"];
            _projectId = _config["Firebase:ProjectId"];
            _httpClientFactory = httpClientFactory;
            _redisService = redisService;
        }

        private async Task<GoogleCredential> GetOrCreateCredentialAsync()
        {
            if (_cachedCredential != null)
            {
                return _cachedCredential;
            }

            await _credentialLock.WaitAsync();
            try
            {
                if (_cachedCredential != null)
                {
                    return _cachedCredential;
                }

                var basePath = AppContext.BaseDirectory;
                var fullPath = Path.Combine(basePath, _serviceAccountRelativePath);

                _logger.LogInformation("Loading Firebase credentials from: {Path}", fullPath);

                _cachedCredential = GoogleCredential
                    .FromFile(fullPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

                return _cachedCredential;
            }
            finally
            {
                _credentialLock.Release();
            }
        }

        public async Task SendNotificationAsync(
    string deviceToken,
    string title,
    string body,
    int chargingRequestID,
    string notificationType,
    Dictionary<string, string>? extraData = null)
        {
            try
            {
                var credential = await GetOrCreateCredentialAsync();

                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                // ✅ جهّز الداتا الأساسية
                var data = new Dictionary<string, string>
                {
                    ["requestId"] = chargingRequestID.ToString(),
                    ["NotificationType"] = notificationType
                };

                // ✅ ضمّ أي extraData جاية من السيرفيس (processId, amounts, ... إلخ)
                if (extraData != null)
                {
                    foreach (var kv in extraData)
                    {
                        // لو نفس الـ key موجود، بنعمل override بالقيمة الجديدة
                        data[kv.Key] = kv.Value;
                    }
                }

                // ✅ إعداد الـ payload النهائي مع Sound + Android Channel + APNS
                var payload = new
                {
                    message = new
                    {
                        token = deviceToken,
                        notification = new
                        {
                            title = title,
                            body = body,
                            sound = "default"
                        },
                        android = new
                        {
                            priority = "high",
                            notification = new
                            {
                                sound = "default",
                                channel_id = "voltyks_notifications_v2",
                                click_action = "FLUTTER_NOTIFICATION_CLICK"
                            }
                        },
                        apns = new
                        {
                            payload = new
                            {
                                aps = new
                                {
                                    sound = "default",
                                    badge = 1
                                }
                            }
                        },
                        data = data
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                _logger.LogInformation("Sending FCM to token: {Token}", deviceToken);
                _logger.LogInformation("Payload: {Payload}", json);

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClientFactory.CreateClient().SendAsync(request);
                _logger.LogInformation("Status Code: {Code}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Firebase Error Response: {Error}", error);

                    var isUnregistered =
                        error.Contains("\"errorCode\":\"UNREGISTERED\"", StringComparison.OrdinalIgnoreCase) ||
                        error.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase);

                    if (response.StatusCode == HttpStatusCode.NotFound && isUnregistered)
                    {
                        try
                        {
                            if (_redisService != null)
                            {
                                var keys = await _redisService.GetAllKeysAsync("fcm_tokens:*");
                                foreach (var key in keys)
                                {
                                    var csv = await _redisService.GetAsync(key);
                                    if (csv?.Contains(deviceToken) == true)
                                    {
                                        var updated = string.Join(",", csv.Split(',')
                                            .Where(t => t.Trim() != deviceToken && !string.IsNullOrWhiteSpace(t)));

                                        if (string.IsNullOrWhiteSpace(updated))
                                            await _redisService.RemoveAsync(key);
                                        else
                                            await _redisService.SetAsync(key, updated, TimeSpan.FromDays(30));
                                    }
                                }
                            }

                            _logger.LogWarning("⚠️ Removed UNREGISTERED FCM token: {Token}", deviceToken);
                        }
                        catch (Exception ex2)
                        {
                            _logger.LogError(ex2, "Failed to remove invalid FCM token from Redis");
                        }

                        // منرجعش Exception، بس بنوقف الإرسال للتوكِن ده
                        return;
                    }

                    throw new Exception($"Firebase Error: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SendNotificationAsync");
                throw;
            }
        }
    }
}
