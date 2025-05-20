using FluentAssertions;
using NUnit.Framework;

namespace UserApi.UnitTests.Services.UserAccountService;

public class GetGroupIdFromSettingsTests : UserAccountServiceTestsBase
{
    
    [Test]
    public void Should_return_groupId_value_for_judge_from_settings()
    {
        var result = Service.GetGroupIdFromSettings(nameof(Settings.AdGroup.VirtualRoomJudge));
        result.Should().Be(Settings.AdGroup.VirtualRoomJudge);
    }

    [Test]
    public void Should_return_null_or_empty_value_for_judge_from_settings()
    {
        var result = Service.GetGroupIdFromSettings("Judge");
        result.Should().BeNullOrEmpty();
    }
}