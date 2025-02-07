using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing.Common.Configuration;
using UserApi.Client;
using UserApi.Common.Security;

namespace UserApi.AcceptanceTests.SmokeTests;

public abstract class UserControllerBase
{
    protected IUserApiClient UserApiClient;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var apiToken = await GenerateApiToken();
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("bearer", apiToken);
        UserApiClient =  UserApi.Client.UserApiClient.GetClient(TestConfig.Instance.VhServices.UserApiUrl, httpClient);
    }
    
    private static async Task<string> GenerateApiToken()
    {
        var azureAdConfig = TestConfig.Instance.AzureAd;
        var vhServicesConfig = TestConfig.Instance.VhServices;

        return await new TokenProvider(TestConfig.Instance.AzureAd).GetClientAccessToken(
            azureAdConfig.ClientId, azureAdConfig.ClientSecret,
            vhServicesConfig.UserApiResourceId);
    }
}