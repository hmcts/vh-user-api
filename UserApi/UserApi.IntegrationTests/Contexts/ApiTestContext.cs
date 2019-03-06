using System.Net.Http;
using Microsoft.AspNetCore.TestHost;

namespace UserApi.IntegrationTests.Contexts
{
    public class ApiTestContext
    {
        public TestServer Server { get; set; }
        public string DbString { get; set; }
        public string BearerToken { get; set; }
        public string Uri { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public StringContent StringContent { get; set; }
        public HttpContent HttpContent { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public string NewUserId { get; set; }
        public string GraphApiToken { get; set; }
    }
}
