using System.Collections.Generic;
using System.Net.Http;
using AcceptanceTests.Common.Configuration.Users;
using Microsoft.AspNetCore.TestHost;
using Testing.Common;
using UserApi.IntegrationTests.Configuration;

namespace UserApi.IntegrationTests.Contexts
{
    public class TestContext
    {
        public TestServer Server { get; set; }
        public string DbString { get; set; }
        public string BearerToken { get; set; }
        public string Uri { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public StringContent StringContent { get; set; }
        public HttpContent HttpContent { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public string NewGroupId { get; set; }
        public string GraphApiToken { get; set; }
        public TestSettings TestSettings { get; set; }
        public Test Test { get; set; }
        public List<UserAccount> UserAccounts { get; set; }
    }
}
