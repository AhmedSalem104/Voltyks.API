using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Voltyks.Application.Interfaces.Firebase;

namespace Voltyks.Application.Services.Firebase
{
    public class FirebaseService : IFirebaseService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _projectId;

        public FirebaseService(IConfiguration config)
        {
            _config = config;
            _projectId = _config["Firebase:ProjectId"];

            _httpClient = new HttpClient();
        }

        public async Task SendNotificationAsync(string deviceToken, string title, string body)
        {
            var serviceAccountPath = _config["Firebase:ServiceAccountFile"];

            var credential = GoogleCredential
                .FromFile(serviceAccountPath)
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
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Firebase Error: {response.StatusCode} - {error}");
            }
        }
    }
}
