using Moq;
using NUnit.Framework;
using UserApi.Services.Interfaces;

namespace UserApi.UnitTests.Services.UserAccountService;

public abstract class UserAccountServiceTestsBase
{
    protected Mock<IGraphUserClient> GraphClient;
    protected UserApi.Services.UserAccountService Service;
    protected Settings Settings;
    
    [SetUp]
    public void Setup()
    {
        Settings = new Settings
        {
            ReformEmail = "example.com",
            IsLive = false,
            AdGroup = new AdGroup
            {
                VirtualRoomJudge = "VirtualRoomJudgeGroupId",
            }
        };
        GraphClient = new Mock<IGraphUserClient>();
        Service = new UserApi.Services.UserAccountService(GraphClient.Object, Settings);
    }
}