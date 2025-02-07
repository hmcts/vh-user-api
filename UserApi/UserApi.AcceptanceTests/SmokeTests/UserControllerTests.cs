using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common.Configuration;

namespace UserApi.AcceptanceTests.SmokeTests;
    
[TestFixture]
public class UserControllerSmokeTests : UserControllerBase
{
    [Test]
    public async Task Should_get_user_by_id()
    {
        string userId = TestConfig.Instance.TestSettings.ExistingUserId;
        var userProfileResponse = await UserApiClient.GetUserByAdUserIdAsync(userId);
        userProfileResponse.UserId.Should().Be(userId);
        userProfileResponse.FirstName.Should().NotBeNullOrWhiteSpace();
        userProfileResponse.DisplayName.Should().NotBeNullOrWhiteSpace();
    }
}