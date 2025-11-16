using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services.Firebase
{
    public class FirebaseService : IFirebaseService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _projectId;
        private readonly ILogger<FirebaseService> _logger;
        private readonly string _serviceAccountRelativePath;
        private readonly IRedisService _redisService;



        public FirebaseService(IConfiguration config, ILogger<FirebaseService> logger, IRedisService redisService)
        {
            _config = config;
            _logger = logger;
            _serviceAccountRelativePath = _config["Firebase:ServiceAccountFile"];
            _projectId = _config["Firebase:ProjectId"];
            _httpClient = new HttpClient();
            _redisService = redisService;
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
                var basePath = AppContext.BaseDirectory;
                var fullPath = Path.Combine(basePath, _serviceAccountRelativePath);

                _logger.LogInformation("Loading Firebase credentials from: {Path}", fullPath);

                var credential = GoogleCredential
                    .FromFile(fullPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

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

                // ✅ إعداد الـ payload النهائي
                var payload = new
                {
                    message = new
                    {
                        token = deviceToken,
                        notification = new
                        {
                            title = title,
                            body = body
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

                var response = await _httpClient.SendAsync(request);
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


        //public async Task SendNotificationAsync(string deviceToken, string title, string body, int chargingRequestID, string notificationType, Dictionary<string, string>? extraData = null)
        //{
        //    try
        //    {
        //        var basePath = AppContext.BaseDirectory;
        //        var fullPath = Path.Combine(basePath, _serviceAccountRelativePath);

        //        _logger.LogInformation("Loading Firebase credentials from: {Path}", fullPath);

        //        var credential = GoogleCredential
        //            .FromFile(fullPath)
        //            .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

        //        var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

        //        // ✅ إعداد الـ payload
        //        var payload = new
        //        {
        //            message = new
        //            {
        //                token = deviceToken,
        //                notification = new
        //                {
        //                    title = title,
        //                    body = body
        //                },
        //                data = new
        //                {
        //                    requestId = chargingRequestID.ToString(),
        //                    NotificationType = notificationType,
        //                    processId = de
        //                }
        //            }
        //        };

        //        var json = JsonSerializer.Serialize(payload);
        //        _logger.LogInformation("Sending FCM to token: {Token}", deviceToken);
        //        _logger.LogInformation("Payload: {Payload}", json);

        //        var request = new HttpRequestMessage(HttpMethod.Post,
        //            $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send");

        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await _httpClient.SendAsync(request);
        //        _logger.LogInformation("Status Code: {Code}", response.StatusCode);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var error = await response.Content.ReadAsStringAsync();
        //            _logger.LogError("Firebase Error Response: {Error}", error);

        //            // ✅ تحليل الخطأ
        //            var isUnregistered =
        //                error.Contains("\"errorCode\":\"UNREGISTERED\"", StringComparison.OrdinalIgnoreCase) ||
        //                error.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase);

        //            // ✅ لو التوكِن مش صالح نحذفه فورًا من Redis
        //            if (response.StatusCode == HttpStatusCode.NotFound && isUnregistered)
        //            {
        //                try
        //                {
        //                    // ⚠️ هنا بنحاول نحذف التوكِن القديم من Redis
        //                    // لازم يكون عندك redisService متاح في FirebaseService (Inject it في الـ constructor)
        //                    if (_redisService != null)
        //                    {

        //                        var keys = await _redisService.GetAllKeysAsync("fcm_tokens:*");
        //                        foreach (var key in keys)
        //                        {
        //                            var csv = await _redisService.GetAsync(key);
        //                            if (csv?.Contains(deviceToken) == true)
        //                            {
        //                                var updated = string.Join(",", csv.Split(',')
        //                                    .Where(t => t.Trim() != deviceToken && !string.IsNullOrWhiteSpace(t)));
        //                                if (string.IsNullOrWhiteSpace(updated))
        //                                    await _redisService.RemoveAsync(key);
        //                                else
        //                                    await _redisService.SetAsync(key, updated, TimeSpan.FromDays(30));
        //                            }
        //                        }
        //                    }
        //                    _logger.LogWarning("⚠️ Removed UNREGISTERED FCM token: {Token}", deviceToken);
        //                }
        //                catch (Exception ex2)
        //                {
        //                    _logger.LogError(ex2, "Failed to remove invalid FCM token from Redis");
        //                }

        //                // منرجعش Exception، نوقف فقط الإرسال لهذا التوكِن
        //                return;
        //            }

        //            // غير كده = خطأ فعلي من Firebase
        //            throw new Exception($"Firebase Error: {response.StatusCode} - {error}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Exception in SendNotificationAsync");
        //        throw; // عشان يظهر الخطأ للأعلى لو لازم
        //    }
        //}


    }
}
