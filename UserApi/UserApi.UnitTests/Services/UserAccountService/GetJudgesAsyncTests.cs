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
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetJudgesAsyncTests: UserAccountServiceTests
    {
        private readonly string _groupId = "Test123";
        private GraphQueryResponse _graphQueryResponse;
        private string _judgesGroup;
        private string _judgesTestGroup;
        private string _accessUri;
        private Group _group;

        [SetUp]
        public void TestInitialize()
        {
            _graphQueryResponse = new GraphQueryResponse() { Value = new List<Group>() };
            _group = new Group() { Id = _groupId };

            _judgesGroup = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq 'VirtualRoomJudge'";
            _judgesTestGroup = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq 'TestAccount'";


            _accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups/{_groupId}/members/microsoft.graph.user?" +
                         "$select=id,userPrincipalName,displayName,givenName,surname&$top=999";
        }

        [Test]
        public async Task Should_return_judges_list_successfully()
        {            
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesTestGroup))
                            .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));
             
            
            var response = await _service.GetJudgesAsync();

            response.Should().NotBeNull();
        }

        [Test]
        public async Task Should_not_exclude_judges_when_settings_is_not_live()
        {
            _graphQueryResponse.Value.Add(_group);
            
            _secureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            var users = new List<User> 
            { 
                new User { Id = "Test123", DisplayName = "T Tester", GivenName = "Test", Surname = "Tester" },
                new User { Id = "Test124", DisplayName = "T Test", GivenName = "Tester", Surname = "Test" }
            };
            
            var directoryObject = new DirectoryObject { AdditionalData = new Dictionary<string, object>() };
            
            directoryObject.AdditionalData.Add("value", JsonConvert.SerializeObject(users));

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            _service = new UserApi.Services.UserAccountService
            (
                _secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, new Settings
                {
                    IsLive = false
                }
            );

            var response = (await _service.GetJudgesAsync()).ToList();

            response.Count.Should().Be(2);
            response.First().DisplayName.Should().Be("T Test");
            response.Last().DisplayName.Should().Be("T Tester");
            
            _secureHttpRequest.Verify(s => s.GetAsync(_graphApiSettings.AccessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public async Task Should_call_graph_api_two_times_following_nextlink()
        {
            _graphQueryResponse.Value.Add(_group);
            
            _secureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            var users1 = new List<User> 
            { 
                new User { Id = "Test123", DisplayName = "T Tester", GivenName = "Test", Surname = "Tester" },
                new User { Id = "Test124", DisplayName = "T Test", GivenName = "Tester", Surname = "Test" }
            };
            
            var users2 = new List<User> 
            { 
                new User { Id = "Test123", DisplayName = "T Tester", GivenName = "Test", Surname = "Tester" },
                new User { Id = "Test124", DisplayName = "T Test", GivenName = "Tester", Surname = "Test" }
            };
            
            var directoryObject1 = new DirectoryObject { AdditionalData = new Dictionary<string, object>() };
            directoryObject1.AdditionalData.Add("value", JsonConvert.SerializeObject(users1));
            directoryObject1.AdditionalData.Add("@odata.nextLink", "someLinkToNextPage");
            
            var directoryObject2 = new DirectoryObject { AdditionalData = new Dictionary<string, object>() };
            directoryObject2.AdditionalData.Add("value", JsonConvert.SerializeObject(users2));

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject1, HttpStatusCode.OK));
            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, "someLinkToNextPage"))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject2, HttpStatusCode.OK));

            _service = new UserApi.Services.UserAccountService
            (
                _secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, new Settings
                {
                    IsLive = false
                }
            );

            var response = (await _service.GetJudgesAsync()).ToList();

            response.Count.Should().Be(4);
            
            _secureHttpRequest.Verify(s => s.GetAsync(_graphApiSettings.AccessToken, _accessUri), Times.Once);
            _secureHttpRequest.Verify(s => s.GetAsync(_graphApiSettings.AccessToken, "someLinkToNextPage"), Times.Once);
        }

        [Test]
        public async Task Should_return_empty_for_not_found_status_code()
        {
            _graphQueryResponse.Value.Add(_group);
            
            _secureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(string.Empty, HttpStatusCode.NotFound));

            _service = new UserApi.Services.UserAccountService
            (
                _secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, new Settings
                {
                    IsLive = false
                }
            );

            var response = await _service.GetJudgesAsync();

            response.Should().BeEmpty();
            
            _secureHttpRequest.Verify(s => s.GetAsync(_graphApiSettings.AccessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public void Should_return_user_exception_for_other_responses()
        {
            _graphQueryResponse.Value.Add(_group);
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));

            const string reason = "User not authorised";

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.GetJudgesAsync());

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get users for group {_groupId}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
