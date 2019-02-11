using System.Net.Http;
using System.Threading.Tasks;
using UserApi.Security;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testing.Common;
using UserApi.Common;

namespace UserApi.IntegrationTests.Controllers
{
    [Parallelizable(ParallelScope.All)]
    public abstract class ControllerTestsBase
    {
        private TestServer _server;
        private string _bearerToken;
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
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();
            
            var configRoot = configRootBuilder.Build();

            var testSettingsOptions = Options.Create(configRoot.GetSection("Testing").Get<TestSettings>());
            var testSettings = testSettingsOptions.Value;

            var azureAdConfigOptions = Options.Create(configRoot.GetSection("AzureAd").Get<AzureAdConfiguration>());
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