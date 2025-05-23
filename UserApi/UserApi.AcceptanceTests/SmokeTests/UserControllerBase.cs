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
    protected string NewUserId;
    private string _graphApiToken;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var apiToken = await GenerateApiToken();
        _graphApiToken = await GenerateGraphApiToken();
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", apiToken);
        UserApiClient =  UserApi.Client.UserApiClient.GetClient(TestConfig.Instance.VhServices.UserApiUrl, httpClient);
    }

    private static async Task<string> GenerateGraphApiToken()
    {
        var clientId = TestConfig.Instance.AzureAd.ClientId;
        var secret = TestConfig.Instance.AzureAd.ClientSecret;
        return await new TokenProvider(TestConfig.Instance.AzureAd).GetClientAccessToken(clientId, secret, "https://graph.microsoft.com");
    }

    private static async Task<string> GenerateApiToken()
    {
        var azureAdConfig = TestConfig.Instance.AzureAd;
        var vhServicesConfig = TestConfig.Instance.VhServices;
        
        return await new TokenProvider(TestConfig.Instance.AzureAd).GetClientAccessToken(
            azureAdConfig.ClientId, azureAdConfig.ClientSecret,
            vhServicesConfig.UserApiResourceId);
    }
    
           
    [TearDown]
    public async Task ClearUp()
    {
        if (string.IsNullOrWhiteSpace(NewUserId)) return;
        TestContext.WriteLine($"Attempting to delete account {NewUserId}");
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _graphApiToken);
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, $@"https://graph.microsoft.com/v1.0/users/{NewUserId}");
        await client.SendAsync(httpRequestMessage);
        NewUserId = null;
    }
}