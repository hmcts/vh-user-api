using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace UserApi.Helper
{
    public interface ISecureHttpRequest
    {
        Task<HttpResponseMessage> GetAsync(string accessToken, string accessUri);

        Task<HttpResponseMessage> PatchAsync(string accessToken, StringContent stringContent, string accessUri);
        
        Task<HttpResponseMessage> PostAsync(string accessToken, StringContent stringContent, string accessUri);
    }

    public class SecureHttpRequest : ISecureHttpRequest
    {
        public async Task<HttpResponseMessage> GetAsync(string accessToken, string accessUri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var httpRequestMessage =
                    new HttpRequestMessage(HttpMethod.Get, accessUri);
                return await client.SendAsync(httpRequestMessage);
            }
        }

        public Task<HttpResponseMessage> PatchAsync(string accessToken, StringContent stringContent, string accessUri)
        {
            return SendAsync(accessToken, stringContent, accessUri, HttpMethod.Patch);
        }

        public Task<HttpResponseMessage> PostAsync(string accessToken, StringContent stringContent, string accessUri)
        {
            return SendAsync(accessToken, stringContent, accessUri, HttpMethod.Post);
        }

        private async Task<HttpResponseMessage> SendAsync(string accessToken, StringContent stringContent, string accessUri, HttpMethod httpMethod)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var httpRequestMessage = new HttpRequestMessage(httpMethod, accessUri);
                httpRequestMessage.Content = stringContent;
                return await client.SendAsync(httpRequestMessage);
            }
        }
    }
}
