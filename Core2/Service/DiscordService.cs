using Core2.HttpClientFactory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core2.Service
{
    public class DiscordService
    {
        private HttpClient _httpClient;
        public DiscordService()
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
        }
        public async Task SendMessage(string message)
        {
            string webhookUrl = Environment.GetEnvironmentVariable("DiscordHook", EnvironmentVariableTarget.Machine);
            var payload = new
            {
                content = message
            };
            var messageJson = JsonSerializer.Serialize(payload);
            var content = new StringContent(messageJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content);
        }
    }
}
