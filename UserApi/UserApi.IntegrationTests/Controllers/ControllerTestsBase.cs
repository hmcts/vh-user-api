using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using Testing.Common;
using UserApi.Security;

namespace UserApi.IntegrationTests.Controllers
{
    [Parallelizable(ParallelScope.All)]
    public abstract class ControllerTestsBase
    {
        private string _userApiToken;
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

            var tokenProvider = new TokenProvider(TestConfig.Instance.AzureAd);
            
            _userApiToken = tokenProvider.GetClientAccessToken
            (
                TestConfig.Instance.AzureAd.TenantId,
                TestConfig.Instance.AzureAd.ClientId,
                TestConfig.Instance.AzureAd.ClientSecret,
                new []{ $"{TestConfig.Instance.AzureAd.VhUserApiResourceId}/.default"}
            );

            GraphApiToken = tokenProvider.GetClientAccessToken
            (
                TestConfig.Instance.AzureAd.TenantId,
                TestConfig.Instance.AzureAd.ClientId,
                TestConfig.Instance.AzureAd.ClientSecret,
                new []{ $"{TestConfig.Instance.AzureAd.GraphApiBaseUri}.default"}
            );
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
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_userApiToken}");
                return await client.GetAsync(uri);
            }
        }

        protected async Task<HttpResponseMessage> SendPostRequestAsync(string uri, HttpContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_userApiToken}");
                return await client.PostAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPatchRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_userApiToken}");
                return await client.PatchAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPutRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_userApiToken}");
                return await client.PutAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendDeleteRequestAsync(string uri)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_userApiToken}");
                return await client.DeleteAsync(uri);
            }
        }
    }
}