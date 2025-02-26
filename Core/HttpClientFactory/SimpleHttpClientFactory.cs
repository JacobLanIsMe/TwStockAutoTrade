using System.Net.Http;

namespace Core.HttpClientFactory
{
    public class SimpleHttpClientFactory
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public HttpClient CreateClient()
        {
            return _httpClient;
        }
    }
}
