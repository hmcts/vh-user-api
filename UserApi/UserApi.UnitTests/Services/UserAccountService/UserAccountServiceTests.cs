using FluentAssertions;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UserApi.Common;
using UserApi.Contract.Responses;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class UserAccountServiceTests
    {
        protected const string Domain = "@hearings.test.server.net";

        protected Mock<ISecureHttpRequest> SecureHttpRequest;
        protected GraphApiSettings GraphApiSettings;
        protected Mock<IIdentityServiceApiClient> IdentityServiceApiClient;
        protected UserApi.Services.UserAccountService Service;
        protected GraphUserResponse GraphUserResponse;
        protected GraphQueryResponse<GraphUserResponse> GraphQueryResponse;
        private Settings _settings;
        protected string Filter;
        protected DirectoryObject DirectoryObject;

        [SetUp]
        public void Setup()
        {
            SecureHttpRequest = new Mock<ISecureHttpRequest>();

            _settings = new Settings { IsLive = true, ReformEmail = Domain.Replace("@", ""),
                    AdGroup = new AdGroup { VirtualRoomJudge = Guid.NewGuid().ToString()}
            };

            var azureAdConfig = new AzureAdConfiguration()
            {
                ClientId = "TestClientId",
                ClientSecret = "TestSecret",
                Authority = "https://Test/Authority",
            };
            var tokenProvider = new Mock<ITokenProvider>();
            GraphApiSettings = new GraphApiSettings(tokenProvider.Object, azureAdConfig);
            IdentityServiceApiClient = new Mock<IIdentityServiceApiClient>();

            GraphUserResponse = new GraphUserResponse()
            {
                Id = "1",
                DisplayName = "T Tester",
                GivenName = "Test",
                Surname = "Tester",
                OtherMails = new List<string>(),
                UserPrincipalName = "TestUser"
            };
            GraphQueryResponse = new GraphQueryResponse<GraphUserResponse> { Value = new List<GraphUserResponse> { GraphUserResponse } };

            var additionalData = new Dictionary<string, object>();

            var jobject = new JObject
            {
                new JProperty("@odata.type","#microsoft.graph.group"),
                new JProperty("startDateTime",DateTime.UtcNow),
                new JProperty("endDateTime", DateTime.UtcNow.AddYears(1)),
                new JProperty("secretText", "The passwords must be 16-64 characters in length"),
                new JProperty("keyId", Guid.NewGuid().ToString()),
                new JProperty("hint", "something")
            };

            additionalData.Add("value", new List<JObject> { jobject });
            DirectoryObject = new DirectoryObject
            {
                Id = "1",
                AdditionalData = additionalData,
                ODataType = "@odata.type"
            };
            

            Service = new UserApi.Services.UserAccountService(SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings);
        }

        protected string AccessUri => $"{GraphApiSettings.GraphApiBaseUri}v1.0/{GraphApiSettings.TenantId}/users?$filter={Filter}&" + 
                                      "$select=id,displayName,userPrincipalName,givenName,surname,otherMails,contactEmail,mobilePhone";

        [Test]
        public async Task Should_increment_the_username()
        {
            const string firstName = "Existing";
            const string lastName = "User";
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();

            // given api returns
            var existingUsers = new[] { "existing.user", "existing.user1" };
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName);

            nextAvailable.Should().Be("existing.user2" + Domain);
            IdentityServiceApiClient.Verify(i => i.GetUsernamesStartingWithAsync(baseUsername), Times.Once);
        }

        [Test]
        public async Task Should_sanitise_names()
        {
            const string firstName = ".First.";
            const string lastName = ".La.st.";
            var baseUsername = "First.La.st".ToLowerInvariant();

            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName);

            nextAvailable.Should().Be(baseUsername + Domain);
            IdentityServiceApiClient.Verify(i => i.GetUsernamesStartingWithAsync(baseUsername), Times.Once);
        }

        [Test]
        public async Task Should_delete()
        {
            IdentityServiceApiClient.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await Service.DeleteUserAsync("User");

            IdentityServiceApiClient.Verify(i => i.DeleteUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Should_update_user_data_in_aad()
        {
            const string newPassword = "newPassword";
            
            IdentityServiceApiClient.Setup(x => x.UpdateUserPasswordAsync(It.IsAny<string>()))
                .ReturnsAsync(newPassword);

            var result = await Service.UpdateUserPasswordAsync("known.user@gmail.com");

            result.Should().Be(newPassword);

            IdentityServiceApiClient.Verify(i => i.UpdateUserPasswordAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Should_get_judges_except_test_judges()
        {
            var judgesList = new List<UserResponse>{ new UserResponse { Email = "aa@hmcts.net" },
            new UserResponse { Email = "bb@hmcts.net" }, new UserResponse{Email="test@hmcts.net"} };

            var testJudges = new List<UserResponse> { new UserResponse { Email = "test@hmcts.net" } };

            var result = judgesList.Except(testJudges, UserApi.Services.UserAccountService.CompareJudgeById).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("aa@hmcts.net", result[0].Email);
            Assert.AreEqual("bb@hmcts.net", result[1].Email);

        }

        [Test]
        public async Task Should_filter_users()
        {
            var serialised = JsonConvert.SerializeObject(GraphQueryResponse);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetUserByFilterAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual("1", result.Id);
            Assert.AreEqual("T Tester", result.DisplayName);
            Assert.AreEqual("Test", result.GivenName);
            Assert.AreEqual("Tester", result.Surname);
            Assert.AreEqual("TestUser", result.UserPrincipalName);
        }

        [Test]
        public async Task Should_filter_users_and_return_null_if_no_users()
        {
            GraphQueryResponse = new GraphQueryResponse<GraphUserResponse>();
            var serialised = JsonConvert.SerializeObject(GraphQueryResponse);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetUserByFilterAsync("user@test.aa");

            Assert.IsNull(result);
        }

        [Test]
        public async Task Should_get_group_for_user()
        {
            var serialised = JsonConvert.SerializeObject(DirectoryObject);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetGroupsForUserAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task Should_get_empty_group_for_user_with_two_records_for_object_type()
        {
            var additionalData = new Dictionary<string, object>();
            var jobject = new JObject
            {
                new JProperty("dif",DateTime.UtcNow),
            };

            additionalData.Add("value", new List<JObject> { jobject });
            var directoryObject = new DirectoryObject
            {
                Id = "1",
                AdditionalData = additionalData,
                ODataType = "@odata.type"
            };

            var serialised = JsonConvert.SerializeObject(directoryObject);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetGroupsForUserAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_with_no_prefix()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "adam.green" });
            var result = await Service.CheckForNextAvailableUsernameAsync("Adam","Green");

            Assert.IsTrue(result.Contains("adam.green@hearings.test.server.net"));
        }
        
        [Test]
        public async Task Should_remove_spaces_before_checking_user_name()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "janemary.vangreen" });
            var result = await Service.CheckForNextAvailableUsernameAsync("Jane Mary","van Green");

            Assert.IsTrue(result.Contains("janemary.vangreen@hearings.test.server.net"));
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_find_with_prefix()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "adam.green2@hearings.test.server.net", "adam.green@hearings.test.server.net", "adam.green1@hearings.test.server.net" });
            var result = await Service.CheckForNextAvailableUsernameAsync("Adam", "Green");

            Assert.IsTrue(result.Contains("adam.green3@hearings.test.server.net"));
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_find_null()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> ());
            var result = await Service.CheckForNextAvailableUsernameAsync("Adam", "Green");

            Assert.IsTrue(result.Contains("adam.green@hearings.test.server.net"));
        }

        [Test]
        public void Should_return_groupId_value_for_judge_from_settings()
        {
            var result = Service.GetGroupIdFromSettings(nameof(_settings.AdGroup.VirtualRoomJudge));
            result.Should().Be(_settings.AdGroup.VirtualRoomJudge);
        }

        [Test]
        public void Should_return_null_or_empty_value_for_judge_from_settings()
        {
            var result = Service.GetGroupIdFromSettings("Judge");
            result.Should().BeNullOrEmpty();
        }

    }
}
