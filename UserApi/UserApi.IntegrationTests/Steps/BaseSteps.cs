using UserApi.IntegrationTests.Contexts;

namespace UserApi.IntegrationTests.Steps
{
    public abstract class BaseSteps
    {      
        protected static async Task<HttpResponseMessage> SendGetRequestAsync(TestContext testContext)
        {
            using var client = testContext.CreateClient();
            return await client.GetAsync(testContext.Uri);
        }

        protected static async Task<HttpResponseMessage> SendPatchRequestAsync(TestContext testContext)
        {
            using var client = testContext.CreateClient();
            return await client.PatchAsync(testContext.Uri, testContext.HttpContent);
        }

        protected static async Task<HttpResponseMessage> SendPostRequestAsync(TestContext testContext)
        {
            using var client = testContext.CreateClient();
            return await client.PostAsync(testContext.Uri, testContext.HttpContent);
        }

        protected static async Task<HttpResponseMessage> SendPutRequestAsync(TestContext testContext)
        {
            using var client = testContext.CreateClient();
            return await client.PutAsync(testContext.Uri, testContext.HttpContent);
        }

        protected static async Task<HttpResponseMessage> SendDeleteRequestAsync(TestContext testContext)
        {
            using var client = testContext.CreateClient();
            return await client.DeleteAsync(testContext.Uri);
        }
    }
}
