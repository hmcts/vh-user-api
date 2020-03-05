using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services
{
    public class GetJudgesAsyncTests: UserAccountServiceTests
    {
        private readonly string groupId = "Test123";
        private GraphQueryResponse graphQueryResponse;
        private string JudgesGroup;
        private string JudgesTestGroup;
        private string accessUri;
        private Group group;

        [SetUp]
        public void TestInitialize()
        {
            graphQueryResponse = new GraphQueryResponse() { Value = new List<Microsoft.Graph.Group>() };
            group = new Group() { Id = groupId };

            JudgesGroup = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq 'VirtualRoomJudge'";
            JudgesTestGroup = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq 'TestAccount'";


            accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups/{groupId}/members?$top=999";
        }

        [Test]
        public async Task Should_return_judges_list_successfully()
        {            
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), JudgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse, HttpStatusCode.OK));
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), JudgesTestGroup))
                            .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse, HttpStatusCode.OK));
             
            
            var response = await _service.GetJudgesAsync();

            response.Should().NotBeNull();
        }

        [Test]
        public async Task Should_not_exclude_judges_when_setttings_is_not_live()
        {
            graphQueryResponse.Value.Add(group);
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), JudgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse, HttpStatusCode.OK)); 

            var users = new List<User> { new User { Id = "Test123", DisplayName = "T Tester", GivenName = "Test", Surname = "Tester" },
                                new User { Id = "Test124", DisplayName = "T Test", GivenName = "Tester", Surname = "Test" }
                            };
            var directoryObject = new DirectoryObject() { AdditionalData = new Dictionary<string, object>()  };
            directoryObject.AdditionalData.Add("value", JsonConvert.SerializeObject(users));

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            _service = new UserAccountService(_secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, new Settings() { IsLive = false });

            var response = await _service.GetJudgesAsync();

            response.Count.Should().Be(2);
            response.First().DisplayName.Should().Be("T Test");
            response.Last().DisplayName.Should().Be("T Tester");
        }

        [Test]
        public async Task Should_return_user_exception_for_other_responses()
        {
            graphQueryResponse.Value.Add(group);
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), JudgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse, HttpStatusCode.OK));

            var reason = "User not authorised";

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.GetJudgesAsync());

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get users for group {groupId}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
