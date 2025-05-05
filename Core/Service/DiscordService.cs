using Core.HttpClientFactory;
using Core.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Service
{
    public class DiscordService : IDiscordService
    {
        private HttpClient _httpClient;
        public DiscordService()
        {
            SimpleHttpClientFactory httpClientFactory = new SimpleHttpClientFactory();
            _httpClient = httpClientFactory.CreateClient();
        }
        public async Task SendMessage(string message)
        {
            string webhookUrl = "https://discord.com/api/webhooks/1368806016346755093/hpg7AE5KnMWqr_wpNJrvZlSg1cojFbjSKCTbL7OBlPyd74YsUVnxizygbrNsvvuEBoQt";
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
