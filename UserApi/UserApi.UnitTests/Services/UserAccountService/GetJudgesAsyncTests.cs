using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Moq;
using NUnit.Framework;

namespace UserApi.UnitTests.Services.UserAccountService;

[TestFixture]
public class GetJudgesAsyncTests : UserAccountServiceTestsBase
{
    private string _groupId;

    [SetUp]
    public void TestInitialize()
    {
        _groupId = Guid.NewGuid().ToString();
        Settings.AdGroup = new AdGroup { VirtualRoomJudge = _groupId, TestAccount = "TestGroupId" };
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetJudgesAsync_ShouldExcludeTestAccounts(bool isLive)
    {
        // Arrange
        Settings.IsLive = isLive;
        var testJudge = new User { Id = "test-id", DisplayName = "Test Judge" };
        var liveJudge = new User { Id = "live-id", DisplayName = "Live Judge" };

        GraphClient.SetupSequence(x => x.GetUsersInGroupAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync([testJudge, liveJudge])
            .ReturnsAsync([testJudge]);

        // Act
        var result = await Service.GetJudgesAsync();

        // Assert
        GraphClient.Verify(x => x.GetUsersInGroupAsync(It.IsAny<string>(), CancellationToken.None), Times.AtLeastOnce);
        
        result.Should().Contain(j => j.Id == "live-id");
        if (isLive)
        {
            result.Count.Should().Be(1);
            result.Should().NotContain(j => j.Id == "test-id");
        }
        else
            result.Count.Should().Be(2);
    }
}