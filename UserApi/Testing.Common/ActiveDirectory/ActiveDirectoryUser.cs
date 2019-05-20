using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Polly;

namespace Testing.Common.ActiveDirectory
{
    public static class ActiveDirectoryUser
    {

        public static async Task<bool> IsUserInAGroup(string user, string groupName, string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                    $@"https://graph.microsoft.com/v1.0/users/{user}/memberOf");
                var result = client.SendAsync(httpRequestMessage).Result;
                var content = await result.Content.ReadAsStringAsync();
                return content.Contains(groupName);
            }
        }

        public static bool RemoveTheUserFromTheGroup(string user, string groupId, string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete,
                    $@"https://graph.microsoft.com/v1.0/groups/{groupId}/members/{user}/$ref");
                var result = client.SendAsync(httpRequestMessage).Result;
                return result.IsSuccessStatusCode;
            }
        }

        public static async Task<bool> DeleteTheUserFromAd(string user, string token)
        {
            
            var tenantId = TestConfig.Instance.AzureAd.TenantId;
            var policy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
           
            
            // sometimes the api can be slow to actually allow us to access the created instance, so retry if it fails the first time
            var result = await policy.ExecuteAsync(async () =>
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete,
                    $@"https://graph.microsoft.com/v1.0/{tenantId}/users/{user}");
                    return await client.SendAsync(httpRequestMessage);
                }
            });
            
            return result.IsSuccessStatusCode;
            
        }
    }
}
