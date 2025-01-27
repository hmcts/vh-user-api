using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Testing.Common.Configuration;
using UserApi.Common;
using UserApi.Common.Security;
using UserApi.Security;

namespace UserApi.IntegrationTests.Controllers
{
    [Parallelizable(ParallelScope.None)]
    public abstract class ControllerTestsBase
    {
        private string _bearerToken;
        private TestServer _server;
        protected string GraphApiToken;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            var webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(c => c.AddServerHeader = false)
                .UseEnvironment("Development")
                .UseStartup<Startup>();
            _server = new TestServer(webHostBuilder);

            await GetClientAccessTokenForBookHearingApi();
        }

        private async Task GetClientAccessTokenForBookHearingApi()
        {
            var azureAdConfig = TestConfig.Instance.AzureAd;
            var vhServicesConfig = TestConfig.Instance.VhServices;

            _bearerToken = await new TokenProvider(TestConfig.Instance.AzureAd).GetClientAccessToken(
                azureAdConfig.ClientId, azureAdConfig.ClientSecret,
                vhServicesConfig.UserApiResourceId);

            GraphApiToken = await new TokenProvider(TestConfig.Instance.AzureAd).GetClientAccessToken(
                azureAdConfig.ClientId, azureAdConfig.ClientSecret,
                "https://graph.microsoft.com");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _server.Dispose();
        }

        private static IConfigurationRoot ConfigurationRoot => new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        private static VhServices TestConfiguration => ConfigurationRoot.GetSection("VhServices").Get<VhServices>();

        private HttpClient CreateClient()
        {
            var client = _server.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
            return client;
        }

        protected async Task<HttpResponseMessage> SendGetRequestAsync(string uri)
        {
            using var client = CreateClient();
            return await client.GetAsync(uri);
        }

        protected async Task<HttpResponseMessage> SendPostRequestAsync(string uri, HttpContent httpContent)
        {
            using var client = CreateClient();
            return await client.PostAsync(uri, httpContent);
        }

        protected async Task<HttpResponseMessage> SendPatchRequestAsync(string uri, StringContent httpContent)
        {
            using var client = CreateClient();
            return await client.PatchAsync(uri, httpContent);
        }

        protected async Task<HttpResponseMessage> SendDeleteRequestAsync(string uri)
        {
            using var client = CreateClient();
            return await client.DeleteAsync(uri);
        }
    }
}