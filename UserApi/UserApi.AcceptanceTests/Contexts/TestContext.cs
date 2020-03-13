using System.Collections.Generic;
using AcceptanceTests.Common.Api.Users;
using AcceptanceTests.Common.Configuration.Users;
using RestSharp;
using UserApi.AcceptanceTests.Configuration;

namespace UserApi.AcceptanceTests.Contexts
{
    public class TestContext
    {
        public UserApiManager Api { get; set; }
        public string BearerToken { get; set; }
        public string GraphApiToken { get; set; }
        public IRestRequest Request { get; set; }
        public IRestResponse Response { get; set; }
        public Test Test { get; set; }
        public List<UserAccount> UserAccounts { get; set; }
        public UserApiConfig UserApiConfig { get; set; }
    }
}
