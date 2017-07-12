using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeerBot.Services
{
    internal class ImageSearchService : IImageSearchService
    {
        private static readonly string ApiKey = ConfigurationManager.AppSettings["Cognitive_BingImageSearch_ApiKey"];
        private static readonly string ApiRootUrl = "https://api.cognitive.microsoft.com/bing/v5.0/images/search";

        private readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(ApiRootUrl)
            };
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);
            return client;
        }

        public async Task<Uri> SearchImage(string query)
        {
            var response = await _httpClient.GetAsync($"?q={query}");
            response.EnsureSuccessStatusCode();

            dynamic result = await response.Content.ReadAsAsync<JObject>();
            var url = (string) result.value[0].contentUrl;
            return new Uri(url);
        }
    }
}