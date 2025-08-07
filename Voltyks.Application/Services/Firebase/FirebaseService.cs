using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Firebase;
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


        public FirebaseService(IConfiguration config, ILogger<FirebaseService> logger)
        {
            _config = config;
            _logger = logger;
            _serviceAccountRelativePath = _config["Firebase:ServiceAccountFile"];
            _projectId = _config["Firebase:ProjectId"];
            _httpClient = new HttpClient();
        }


        
        public async Task SendNotificationAsync(string deviceToken, string title, string body , int chargingRequestID)
        {
            try
            {
                var basePath = AppContext.BaseDirectory;
                //var fullPath = Path.Combine(basePath, "Firebase", "service-account-key.json");
                var fullPath = Path.Combine(basePath, _serviceAccountRelativePath);

                _logger.LogInformation("Loading Firebase credentials from: {Path}", fullPath);

                var credential = GoogleCredential
                    .FromFile(fullPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

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
                        data = new
                        {
                            requestId = chargingRequestID.ToString()
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                _logger.LogInformation("Sending FCM to token: {Token}", deviceToken);
                _logger.LogInformation("Payload: {Payload}", json);

                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation("Status Code: {Code}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Firebase Error Response: {Error}", error);
                    throw new Exception($"Firebase Error: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SendNotificationAsync");
                throw; // Important: rethrow it to keep the error visible at upper level
            }
        }

    }
}
