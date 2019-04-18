using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace UserApi.Helper
{
    public interface ISecureHttpRequest
    {
        HttpResponseMessage CreateHttpClientGet(string accessToken, string accessUri);

        Task<HttpResponseMessage> CreateHttpClientGetAsync(string accessToken, string accessUri);

        Task<HttpResponseMessage> CreateHttpClientPatchOrPostAsync(string accessToken, StringContent stringContent, string accessUri, HttpMethod httpMethod);
    }

    public class SecureHttpRequest : ISecureHttpRequest
    {
        public HttpResponseMessage CreateHttpClientGet(string accessToken, string accessUri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var httpRequestMessage =
                    new HttpRequestMessage(HttpMethod.Get, accessUri);
                var responseMessage = client.SendAsync(httpRequestMessage).Result;
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> CreateHttpClientGetAsync(string accessToken, string accessUri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var httpRequestMessage =
                    new HttpRequestMessage(HttpMethod.Get, accessUri);
                return await client.SendAsync(httpRequestMessage);
            }
        }

        public async Task<HttpResponseMessage> CreateHttpClientPatchOrPostAsync(string accessToken, StringContent stringContent, string accessUri, HttpMethod httpMethod)
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
