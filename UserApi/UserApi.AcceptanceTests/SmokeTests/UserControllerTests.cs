using System.Threading.Tasks;
using Bogus;
using Bogus.DataSets;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common.Configuration;
using UserApi.Contract.Requests;

namespace UserApi.AcceptanceTests.SmokeTests;
    
[TestFixture]
public class UserControllerSmokeTests : UserControllerBase
{
    
    private readonly Name _name = new Faker().Name;
    
    [Test]
    public async Task Should_get_user_by_id()
    {
        string userId = TestConfig.Instance.TestSettings.ExistingUserId;
        var userProfileResponse = await UserApiClient.GetUserByAdUserIdAsync(userId);
        userProfileResponse.UserId.Should().Be(userId);
        userProfileResponse.FirstName.Should().NotBeNullOrWhiteSpace();
        userProfileResponse.DisplayName.Should().NotBeNullOrWhiteSpace();
    }
    
    
    [Test]
    public async Task Should_create_citizen_user_on_ad()
    {
        var createUserRequest = new CreateUserRequest
        {
            RecoveryEmail = $"Automation_{_name.FirstName()}@hmcts.net",
            FirstName = $"Automation_{_name.FirstName()}",
            LastName = $"Automation_{_name.LastName()}"
        };

        var createUserResponse = await UserApiClient.CreateUserAsync(createUserRequest);
        NewUserId = createUserResponse.UserId;
        createUserResponse.UserId.Should().NotBeNullOrEmpty();
        createUserResponse.Username.ToLower().Should()
            .Be($@"{createUserRequest.FirstName}.{createUserRequest.LastName}@{TestConfig.Instance.Settings.ReformEmail}".ToLower());
        createUserResponse.OneTimePassword.Should().NotBeNullOrEmpty();
    }
}