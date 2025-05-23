using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Moq;
using NUnit.Framework;
using UserApi.Contract.Responses;

namespace UserApi.UnitTests.Services.UserAccountService;

[TestFixture]
public class UserTestAccountTests : UserAccountServiceTestsBase
{
    private List<User> _testResponse;

    [SetUp]
    public void SetupTest()
    {
        _testResponse =
        [
            new User
            {
                Id = "1",
                UserPrincipalName = "Test User",
                GivenName = "Test",
                Surname = "User",
                Mail = null,
                OtherMails = ["other@mail.com"]
            }
        ];
    }
    
    [Test]  
    public async Task should_fetch_test_accounts()
    {
        GraphClient.Setup(e => e.GetUsersAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(_testResponse);
        var response = await Service.GetTestUsersAsync("Test");
        ValidateResponse(response);
    }
    
    [Test]  
    public async Task should_fetch_test_judge_accounts()
    {
        GraphClient.Setup(e => e.GetUsersInGroupAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(_testResponse);
        var response = await Service.GetTestJudgesAsync();
        ValidateResponse(response);
    }
    
    [Test]  
    public async Task should_fetch_test_panelMember_accounts()
    {
        GraphClient.Setup(e => e.GetUsersInGroupAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(_testResponse);
        var response = await Service.GetPerformancePanelMembersAsync();
        ValidateResponse(response);
    }

    private static void ValidateResponse(List<UserForTestResponse> response)
    {
        response.Should().NotBeNull();
        response.Count.Should().Be(1);
        response[0].Mail.Should().Be("other@mail.com");
        response[0].UserPrincipalName.Should().Be("Test User");
        response[0].GivenName.Should().Be("Test");
        response[0].Surname.Should().Be("User");
    }
}