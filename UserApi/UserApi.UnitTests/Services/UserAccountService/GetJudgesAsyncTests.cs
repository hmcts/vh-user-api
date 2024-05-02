using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Moq;
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
            var jsonResponse = """
                               {
                                 '@odata.context': 'https://graph.microsoft.com/v1.0/$metadata#users(id,otherMails,userPrincipalName,displayName,givenName,surname)',
                                 '@odata.count': 1,
                                 'value': [
                                   {
                                     'id': 'a3d6e5c6-3a70-4ebe-9294-b549f59ff198',
                                     'otherMails': [
                                       'manual.test@test.com'
                                     ],
                                     'userPrincipalName': 'test.judge@test.net',
                                     'displayName': 'Test Judge',
                                     'givenName': 'Test',
                                     'surname': 'Judge'
                                   }
                                 ]
                               }
                               """;
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                            .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessageForJson(jsonResponse, HttpStatusCode.OK));

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
            
            var jsonResponse = """
                               {
                                 '@odata.context': 'https://graph.microsoft.com/v1.0/$metadata#users(id,otherMails,userPrincipalName,displayName,givenName,surname)',
                                 '@odata.count': 2,
                                 'value': [
                                   {
                                     'id': 'a3d6e5c6-3a70-4ebe-9294-b549f59ff198',
                                     'otherMails': [
                                       'manual.test@test.com'
                                     ],
                                     'userPrincipalName': 'test.judge@test.net',
                                     'displayName': 'Test Judge',
                                     'givenName': 'Test',
                                     'surname': 'Judge'
                                   },
                                   {
                                     'id': '110a8327-1fe4-4844-8c17-269bacaf0a96',
                                     'otherMails': [
                                       'manual.test2@test.com'
                                     ],
                                     'userPrincipalName': 'test2.judge2@test.net',
                                     'displayName': 'Test2 Judge2',
                                     'givenName': 'Test2',
                                     'surname': 'Judge2'
                                   }
                                 ]
                               }
                               """;
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessageForJson(jsonResponse, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync()).ToList();

            response.Count.Should().Be(2);
            response[0].DisplayName.Should().Be("Test Judge");
            response[response.Count-1].DisplayName.Should().Be("Test2 Judge2");
            
            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public async Task Should_call_graph_api_two_times_following_nextlink()
        {
            _graphQueryResponse.Value.Add(_group);
            var jsonResponse1 = """
                               {
                                 '@odata.context': 'https://graph.microsoft.com/v1.0/$metadata#users(id,otherMails,userPrincipalName,displayName,givenName,surname)',
                                 '@odata.count': 1,
                                 '@odata.nextLink': 'somenextlink',
                                 'value': [
                                   {
                                     'id': 'a3d6e5c6-3a70-4ebe-9294-b549f59ff198',
                                     'otherMails': [
                                       'manual.test@test.com'
                                     ],
                                     'userPrincipalName': 'test.judge@test.net',
                                     'displayName': 'Test Judge',
                                     'givenName': 'Test',
                                     'surname': 'Judge'
                                   }
                                 ]
                               }
                               """;
            
            var jsonResponse2 = """
                               {
                                 '@odata.context': 'https://graph.microsoft.com/v1.0/$metadata#users(id,otherMails,userPrincipalName,displayName,givenName,surname)',
                                 '@odata.count': 1,
                                 'value': [
                                   {
                                     'id': '110a8327-1fe4-4844-8c17-269bacaf0a96',
                                     'otherMails': [
                                       'manual.test2@test.com'
                                     ],
                                     'userPrincipalName': 'test2.judge2@test.net',
                                     'displayName': 'Test2 Judge2',
                                     'givenName': 'Test2',
                                     'surname': 'Judge2'
                                   }
                                 ]
                               }
                               """;

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessageForJson(jsonResponse1, HttpStatusCode.OK));
            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, "somenextlink"))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessageForJson(jsonResponse2, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync()).ToList();

            response.Count.Should().Be(2);

            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri), Times.Once);
            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, "somenextlink"), Times.Once);
        }

        [Test]
        public async Task Should_return_empty_for_not_found_status_code()
        {
            _graphQueryResponse.Value.Add(_group);
            
            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK)); 

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(string.Empty, HttpStatusCode.NotFound));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = await Service.GetJudgesAsync();

            response.Should().BeEmpty();
            
            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public void Should_return_user_exception_for_other_responses()
        {
            _graphQueryResponse.Value.Add(_group);

            const string reason = "User not authorised";

            SecureHttpRequest.Setup(x => x.GetAsync(GraphApiSettings.AccessToken, _accessUri))
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
            
            var jsonResponse = """
                               {
                                 '@odata.context': 'https://graph.microsoft.com/v1.0/$metadata#users(id,otherMails,userPrincipalName,displayName,givenName,surname)',
                                 '@odata.count': 2,
                                 'value': [
                                   {
                                     'id': 'Test1',
                                     'userPrincipalName': 'judge_117@hmcts.net',
                                     'displayName': 'Judge 117',
                                     'givenName': 'Judge',
                                     'surname': '117'
                                   },
                                   {
                                     'id': 'Test2',
                                     'userPrincipalName': 'judge_23@hmcts.net',
                                     'displayName': 'Judge 23',
                                     'givenName': 'Judge',
                                     'surname': '23'
                                   },
                                   {
                                     'id': 'Test3',
                                     'userPrincipalName': 'judge_16@hmcts.net',
                                     'displayName': 'Judge 16',
                                     'givenName': 'Judge',
                                     'surname': '16'
                                   },
                                 ]
                               }
                               """;
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessageForJson(jsonResponse, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync("117")).ToList();

            response.Count.Should().Be(1);
            response[0].DisplayName.Should().Be("Judge 117");

            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri), Times.Once);
        }
        
        [Test]
        public async Task Should_filter_judges_by_username_case_insensitive()
        {
            _graphQueryResponse.Value.Add(_group);
            
            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), _judgesGroup))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_graphQueryResponse, HttpStatusCode.OK));
            
            var jsonResponse = """
                               {
                                 '@odata.context': 'https://graph.microsoft.com/v1.0/$metadata#users(id,otherMails,userPrincipalName,displayName,givenName,surname)',
                                 '@odata.count': 2,
                                 'value': [
                                   {
                                     'id': 'Test1',
                                     'userPrincipalName': 'judge_ALPHA@hmcts.net',
                                     'displayName': 'Judge Alpha',
                                     'givenName': 'Judge',
                                     'surname': 'Alpha'
                                   },
                                   {
                                     'id': 'Test2',
                                     'userPrincipalName': 'judge_23@hmcts.net',
                                     'displayName': 'Judge 23',
                                     'givenName': 'Judge',
                                     'surname': '23'
                                   },
                                   {
                                     'id': 'Test3',
                                     'userPrincipalName': 'judge_16@hmcts.net',
                                     'displayName': 'Judge 16',
                                     'givenName': 'Judge',
                                     'surname': '16'
                                   },
                                 ]
                               }
                               """;
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), _accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessageForJson(jsonResponse, HttpStatusCode.OK));

            Service = new UserApi.Services.UserAccountService
            (
                SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings
            );

            var response = (await Service.GetJudgesAsync("JUDGE_alpha")).ToList();

            response.Count.Should().Be(1);
            response[0].DisplayName.Should().Be("Judge Alpha");

            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, _accessUri), Times.Once);
        }
    }
}
