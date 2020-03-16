using System.Net.Http;
using System.Threading.Tasks;
using UserApi.IntegrationTests.Contexts;

namespace UserApi.IntegrationTests.Steps
{
    public abstract class BaseSteps
    {      
        protected async Task<HttpResponseMessage> SendGetRequestAsync(TestContext testContext)
        {
            using var client = testContext.Server.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {testContext.Tokens.UserApiBearerToken}");
            return await client.GetAsync(testContext.Uri);
        }

        protected async Task<HttpResponseMessage> SendPatchRequestAsync(TestContext testContext)
        {
            using var client = testContext.Server.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {testContext.Tokens.UserApiBearerToken}");
            return await client.PatchAsync(testContext.Uri, testContext.HttpContent);
        }

        protected async Task<HttpResponseMessage> SendPostRequestAsync(TestContext testContext)
        {
            using var client = testContext.Server.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {testContext.Tokens.UserApiBearerToken}");
            return await client.PostAsync(testContext.Uri, testContext.HttpContent);
        }

        protected async Task<HttpResponseMessage> SendPutRequestAsync(TestContext testContext)
        {
            using var client = testContext.Server.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {testContext.Tokens.UserApiBearerToken}");
            return await client.PutAsync(testContext.Uri, testContext.HttpContent);
        }

        protected async Task<HttpResponseMessage> SendDeleteRequestAsync(TestContext testContext)
        {
            using var client = testContext.Server.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {testContext.Tokens.UserApiBearerToken}");
            return await client.DeleteAsync(testContext.Uri);
        }
    }
}
