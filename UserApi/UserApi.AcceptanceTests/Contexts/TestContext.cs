using RestSharp;
using Testing.Common.Configuration;

namespace UserApi.AcceptanceTests.Contexts
{
    public class TestContext
    {
        public Config Config { get; set; }
        public RestRequest Request { get; set; }
        public IRestResponse Response { get; set; }
        public Test Test { get; set; }
        public UserApiTokens Tokens { get; set; }
        public List<UserAccount> UserAccounts { get; set; }

        public RestClient Client()
        {
            var client = new RestClient(Config.VhServices.UserApiUrl);
            client.AddDefaultHeader("Accept", "application/json");
            client.AddDefaultHeader("Authorization", $"Bearer {Tokens.UserApiBearerToken}");
            return client;
        }

        public static RestRequest Get(string path) => new(path, Method.GET);

        public static RestRequest Patch(string path, object requestBody = null)
        {
            var request = new RestRequest(path, Method.PATCH);
            request.AddParameter("Application/json", ApiRequestHelper.Serialise(requestBody),
                ParameterType.RequestBody);
            return request;
        }
    }
}
