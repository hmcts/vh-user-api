using System.Net.Http;
using System.Threading.Tasks;
using UserApi.Security;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

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
            var webHostBuilder = WebHost.CreateDefaultBuilder().UseStartup<Startup>();
            _server = new TestServer(webHostBuilder);
            GetClientAccessTokenForBookHearingApi();
        }

        private void GetClientAccessTokenForBookHearingApi()
        {
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();
            
            LoadKeyVaultSettings(configRootBuilder);
            
            var configRoot = configRootBuilder.Build();
            
            var securitySettingsOptions = Options.Create(configRoot.Get<SecuritySettings>());
            var securitySettings = securitySettingsOptions.Value;
            _bearerToken = new TokenProvider(securitySettingsOptions).GetClientAccessToken(
                securitySettings.BookHearingUIClientId, securitySettings.BookHearingUIClientSecret,
                securitySettings.BookHearingApiResourceId);

            GraphApiToken = new TokenProvider(securitySettingsOptions).GetClientAccessToken(
                securitySettings.BookHearingUIClientId, securitySettings.BookHearingUIClientSecret,
                "https://graph.microsoft.com");
        }

        private void LoadKeyVaultSettings(IConfigurationBuilder builder)
        {
            var tempConfigRoot = builder.Build();
            var keyVaultSettings = new KeyVaultSettings();
            tempConfigRoot.Bind("KeyVaultSettings", keyVaultSettings);

            builder.AddAzureKeyVault(keyVaultSettings.KeyVaultUri, keyVaultSettings.ClientId,
                keyVaultSettings.ClientSecret);
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