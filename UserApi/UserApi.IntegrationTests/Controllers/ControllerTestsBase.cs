using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using Testing.Common;
using UserApi.Security;

namespace UserApi.IntegrationTests.Controllers
{
    [Parallelizable(ParallelScope.All)]
    public abstract class ControllerTestsBase
    {
        private string _clientApiToken;
        private TestServer _server;
        protected string GraphApiToken;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(c => c.AddServerHeader = false)
                .UseEnvironment("Development")
                .UseStartup<Startup>();
            _server = new TestServer(webHostBuilder);

            GetClientAccessTokenForBookHearingApi();
        }

        private void GetClientAccessTokenForBookHearingApi()
        {
            var settings = TestConfig.Instance;

            _clientApiToken = new TokenProvider().GetClientAccessToken(settings.AzureAd.TenantId, settings.TestSettings.TestClientId, settings.TestSettings.TestClientSecret,
                new string[] { "https://devhearingsreform.onmicrosoft.com/user-api-dev/.default" });

            GraphApiToken = new TokenProvider().GetClientAccessToken(settings.AzureAd.AzureAdGraphApiConfig.TenantId, settings.AzureAd.AzureAdGraphApiConfig.ClientId, settings.AzureAd.AzureAdGraphApiConfig.ClientSecret,
                new string[] { "https://graph.microsoft.com/.default" });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _server.Dispose();
        }

        protected async Task<HttpResponseMessage> SendGetRequestAsync(string uri)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_clientApiToken}");
                return await client.GetAsync(uri);
            }
        }

        protected async Task<HttpResponseMessage> SendPostRequestAsync(string uri, HttpContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_clientApiToken}");
                return await client.PostAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPatchRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_clientApiToken}");
                return await client.PatchAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPutRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_clientApiToken}");
                return await client.PutAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendDeleteRequestAsync(string uri)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_clientApiToken}");
                return await client.DeleteAsync(uri);
            }
        }
    }
}