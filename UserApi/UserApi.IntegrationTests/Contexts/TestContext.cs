using System.Collections.Generic;
using System.Net.Http;
using AcceptanceTests.Common.Configuration.Users;
using Microsoft.AspNetCore.TestHost;
using Testing.Common.Configuration;
using Testing.Common.Helper;

namespace UserApi.IntegrationTests.Contexts
{
    public class TestContext
    {
        public Config Config { get; set; }
        public HttpContent HttpContent { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public TestServer Server { get; set; }
        public Test Test { get; set; }
        public UserApiTokens Tokens { get; set; }
        public string Uri { get; set; }
        public List<UserAccount> UserAccounts { get; set; }        
        public string RequestUrl => Config.VhServices.UserApiUrl + Uri;

        public HttpClient CreateClient()
        {
            HttpClient client;
            if (Zap.SetupProxy)
            {
                var handler = new HttpClientHandler
                {
                    Proxy = Zap.WebProxy,
                    UseProxy = true,
                };

                client = new HttpClient(handler)
                {
                    BaseAddress = new System.Uri(Config.VhServices.UserApiUrl)
                };
            }
            else
            {
                client = Server.CreateClient();
            }

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Tokens.UserApiBearerToken}");
            return client;
        }
    }
}
