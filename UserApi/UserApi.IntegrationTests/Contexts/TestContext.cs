using System.Collections.Generic;
using System.Net.Http;
using AcceptanceTests.Common.Configuration.Users;
using Microsoft.AspNetCore.TestHost;
using RestSharp;
using UserApi.IntegrationTests.Configuration;

namespace UserApi.IntegrationTests.Contexts
{
    public class TestContext
    {
        public string BearerToken { get; set; }
        public string GraphApiToken { get; set; }
        public HttpContent HttpContent { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public IRestRequest Request { get; set; }
        public IRestResponse Response { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public TestServer Server { get; set; }
        public Test Test { get; set; }
        public string Uri { get; set; }
        public List<UserAccount> UserAccounts { get; set; }
        public UserApiConfig UserApiConfig { get; set; }
    }
}
