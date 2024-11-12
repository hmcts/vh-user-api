using System;
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
        private string _groupId;
        private GraphQueryResponse<Group> _graphQueryResponse;
        private string _judgesGroup;
        private string _accessUri;
        private Group _group;
        private Settings _settings;
        
        [SetUp]
        public void TestInitialize()
        {
            _groupId = Guid.NewGuid().ToString();
            _settings = new Settings
            {
                IsLive = false,
                AdGroup = new AdGroup { VirtualRoomJudge = _groupId }
            };

            _graphQueryResponse = new GraphQueryResponse<Group>() { Value = new List<Group>() };
            _group = new Group() { Id = _settings.AdGroup.VirtualRoomJudge };

            _judgesGroup = $"{GraphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq 'Judge'";

            _accessUri = $"{GraphApiSettings.GraphApiBaseUri}v1.0/groups/{_groupId}/members/microsoft.graph.user?$filter=givenName ne null and not(startsWith(givenName, 'TP'))&$count=true" + 
                "&$select=id,otherMails,userPrincipalName,displayName,givenName,surname&$top=999";
        }

        [Test]
        public async Task Should_return_judges_list_successfully()
        {            
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                            .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );
            var response = await Service.GetJudgesAsync();

            response.Should().NotBeNull();
        }

        [Test]
        public async Task Should_not_exclude_judges_when_settings_is_not_live()
        {
            _graphQueryResponse.Value.Add(_group);
            
            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            var users = new List<User> 
            { 
                new User { Id = "Test123", DisplayName = "T Tester", GivenName = "Test", Surname = "Tester" },
                new User { Id = "Test124", DisplayName = "T Test", GivenName = "Tester", Surname = "Test" }
            };
            
            var directoryObject = new DirectoryObject { AdditionalData = new Dictionary<string, object>() };
            
            directoryObject.AdditionalData.Add("value", JsonConvert.SerializeObject(users));

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync()).ToList();

            response.Count.Should().Be(2);
            response[0].DisplayName.Should().Be("T Test");
            response[response.Count-1].DisplayName.Should().Be("T Tester");
            
            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public async Task Should_call_graph_api_two_times_following_nextlink()
        {
            _graphQueryResponse.Value.Add(_group);

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

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject1, HttpStatusCode.OK));
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, "someLinkToNextPage"))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject2, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync()).ToList();

            response.Count.Should().Be(4);

            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, _accessUri), Times.Once);
            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, "someLinkToNextPage"), Times.Once);
        }

        [Test]
        public async Task Should_return_empty_for_not_found_status_code()
        {
            _graphQueryResponse.Value.Add(_group);
            
            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(string.Empty, HttpStatusCode.NotFound));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = await Service.GetJudgesAsync();

            response.Should().BeEmpty();
            
            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public async Task Should_return_user_exception_for_other_responses()
        {
            _graphQueryResponse.Value.Add(_group);

            const string reason = "User not authorised";

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(x => x.GetAsync(accessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetJudgesAsync());

            response.Should().NotBeNull();
            response!.Message.Should().Be($"Failed to get users for group {_groupId}: {reason}");
            response.Reason.Should().Be(reason);
        }

        [Test]
        public async Task Should_filter_judges_by_username()
        {
            _graphQueryResponse.Value.Add(_group);
            
            var users = new List<User> 
            { 
                new User { Id = "Test1", DisplayName = "Judge 117", GivenName = "Judge", Surname = "117", UserPrincipalName = "judge_117@hmcts.net"},
                new User { Id = "Test2", DisplayName = "Judge 23", GivenName = "Judge", Surname = "23", UserPrincipalName = "judge_23@hmcts.net" },
                new User { Id = "Test3", DisplayName = "Judge 16", GivenName = "Judge", Surname = "16", UserPrincipalName = "judge_16@hmcts.net" }
            };
            
            var directoryObject = new DirectoryObject { AdditionalData = new Dictionary<string, object>() };
            
            directoryObject.AdditionalData.Add("value", JsonConvert.SerializeObject(users));

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync("117")).ToList();

            response.Count.Should().Be(1);
            response[0].DisplayName.Should().Be("Judge 117");

            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public async Task Should_filter_judges_by_username_case_insensitive()
        {
            _graphQueryResponse.Value.Add(_group);
            
            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            var users = new List<User> 
            { 
                new User { Id = "Test1", DisplayName = "Judge Alpha", GivenName = "Judge", Surname = "Alpha", UserPrincipalName = "judge_ALPHA@hmcts.net"},
                new User { Id = "Test2", DisplayName = "Judge 23", GivenName = "Judge", Surname = "23", UserPrincipalName = "judge_23@hmcts.net" },
                new User { Id = "Test3", DisplayName = "Judge 16", GivenName = "Judge", Surname = "16", UserPrincipalName = "judge_16@hmcts.net" }
            };
            
            var directoryObject = new DirectoryObject { AdditionalData = new Dictionary<string, object>() };
            
            directoryObject.AdditionalData.Add("value", JsonConvert.SerializeObject(users));

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync("JUDGE_alpha")).ToList();

            response.Count.Should().Be(1);
            response[0].DisplayName.Should().Be("Judge Alpha");

            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, _accessUri), Times.Once);
        }
    }
}