using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using UserApi.Security;
using UserApi.Services.Models;
using UserApi.UnitTests.Helpers;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetJudgesAsyncTests: UserAccountServiceTests
    {
        private const string GroupId = "Test123";
        private GraphQueryResponse _graphQueryResponse;
        private string _judgesGroup;
        private string _judgesTestGroup;
        private string _accessUri;
        private Group _group;

        [SetUp]
        public void TestInitialize()
        {
            _graphQueryResponse = new GraphQueryResponse() { Value = new List<Microsoft.Graph.Group>() };
            _group = new Group() { Id = GroupId };

            _judgesGroup = $"{GraphApiSettings.GraphApiUri}v1.0/groups?$filter=displayName eq 'VirtualRoomJudge'";
            _judgesTestGroup = $"{GraphApiSettings.GraphApiUri}v1.0/groups?$filter=displayName eq 'TestAccount'";


            _accessUri = $"{GraphApiSettings.GraphApiUri}v1.0/groups/{GroupId}/members?$top=999";
        }

        [Test]
        public async Task Should_return_judges_list_successfully()
        {            
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(RequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesTestGroup))
                            .ReturnsAsync(RequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));
             
            
            var response = await Service.GetJudgesAsync();

            response.Should().NotBeNull();
        }

        [Test]
        public async Task Should_not_exclude_judges_when_setttings_is_not_live()
        {
            _graphQueryResponse.Value.Add(_group);
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(RequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            var users = new List<User> { new User { Id = "Test123", DisplayName = "T Tester", GivenName = "Test", Surname = "Tester" },
                                new User { Id = "Test124", DisplayName = "T Test", GivenName = "Tester", Surname = "Test" }
                            };
            var directoryObject = new DirectoryObject() { AdditionalData = new Dictionary<string, object>()  };
            directoryObject.AdditionalData.Add("value", JsonConvert.SerializeObject(users));

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri)).ReturnsAsync(RequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService(SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, new Settings() { IsLive = false });

            var response = await Service.GetJudgesAsync();

            response.Count.Should().Be(2);
            response.First().DisplayName.Should().Be("T Test");
            response.Last().DisplayName.Should().Be("T Tester");
        }

        [Test]
        public async Task Should_return_user_exception_for_other_responses()
        {
            _graphQueryResponse.Value.Add(_group);
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(RequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));

            var reason = "User not authorised";

            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                .ReturnsAsync(RequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetJudgesAsync());

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get users for group {GroupId}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
