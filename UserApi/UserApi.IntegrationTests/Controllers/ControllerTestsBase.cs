using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testing.Common;
using UserApi.Common;
using UserApi.Security;

namespace UserApi.IntegrationTests.Controllers
{
    [Parallelizable(ParallelScope.All)]
    public abstract class ControllerTestsBase
    {
        private string _bearerToken;
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
            var testSettings = TestConfig.Instance.TestSettings;

            var azureAdConfigOptions = Options.Create(TestConfig.Instance.AzureAd);
            var azureAdConfiguration = azureAdConfigOptions.Value;
            _bearerToken = new TokenProvider(azureAdConfigOptions).GetClientAccessToken(
                testSettings.TestClientId, testSettings.TestClientSecret,
                azureAdConfiguration.VhUserApiResourceId);

            GraphApiToken = new TokenProvider(azureAdConfigOptions).GetClientAccessToken(
                testSettings.TestClientId, testSettings.TestClientSecret,
                "https://graph.microsoft.com");
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
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.GetAsync(uri);
            }
        }

        protected async Task<HttpResponseMessage> SendPostRequestAsync(string uri, HttpContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.PostAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPatchRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.PatchAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPutRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.PutAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendDeleteRequestAsync(string uri)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.DeleteAsync(uri);
            }
        }
    }
}