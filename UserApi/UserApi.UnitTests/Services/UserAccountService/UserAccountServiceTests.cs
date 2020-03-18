using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class UserAccountServiceTests
    {
        protected const string Domain = "@hearings.test.server.net";

        protected Mock<ISecureHttpRequest> _secureHttpRequest;
        protected GraphApiSettings _graphApiSettings;
        protected Mock<IIdentityServiceApiClient> _identityServiceApiClient;
        protected UserApi.Services.UserAccountService _service;
        protected AzureAdGraphUserResponse azureAdGraphUserResponse;
        protected AzureAdGraphQueryResponse<AzureAdGraphUserResponse> azureAdGraphQueryResponse;

        private Settings settings;

        protected string filter;
        protected DirectoryObject _directoryObject;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<ISecureHttpRequest>();

            settings = new Settings() { IsLive = true, ReformEmail = Domain.Replace("@", "") };

            var azureAdConfig = new AzureAdConfiguration()
            {
                ClientId = "TestClientId",
                ClientSecret = "TestSecret",
                Authority = "https://Test/Authority",
                VhUserApiResourceId = "TestResourceId"
            };
            var tokenProvider = new Mock<ITokenProvider>();
            _graphApiSettings = new GraphApiSettings(tokenProvider.Object, azureAdConfig);
            _identityServiceApiClient = new Mock<IIdentityServiceApiClient>();

            azureAdGraphUserResponse = new AzureAdGraphUserResponse()
            {
                ObjectId = "1",
                DisplayName = "T Tester",
                GivenName = "Test",
                Surname = "Tester",
                OtherMails = new List<string>(),
                UserPrincipalName = "TestUser"
            };
            azureAdGraphQueryResponse = new AzureAdGraphQueryResponse<AzureAdGraphUserResponse> { Value = new List<AzureAdGraphUserResponse> { azureAdGraphUserResponse } };

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
            _directoryObject = new DirectoryObject
            {
                Id = "1",
                AdditionalData = additionalData,
                ODataType = "@odata.type"
            };

            _service = new UserApi.Services.UserAccountService(_secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, settings);
        }

        protected string AccessUri => $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}/users?$filter={filter}&api-version=1.6";
        }

        [Test]
        public async Task Should_increment_the_username()
        {
            const string firstName = "Existing";
            const string lastName = "User";
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();

            // given api returns
            var existingUsers = new[] { "existing.user", "existing.user1" };
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync(firstName, lastName);

            nextAvailable.Should().Be("existing.user2" + Domain);
            _identityServiceApiClient.Verify(i => i.GetUsernamesStartingWithAsync(baseUsername), Times.Once);
        }

        [Test]
        public async Task Should_delete()
        {
            _identityServiceApiClient.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.DeleteUserAsync("User");

            _identityServiceApiClient.Verify(i => i.DeleteUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Should_update_user_data_in_aad()
        {
            _identityServiceApiClient.Setup(x => x.UpdateUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.UpdateUserAsync("known.user@gmail.com");

            _identityServiceApiClient.Verify(i => i.UpdateUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Should_get_judges_except_test_judges()
        {
            var judgesList = new List<UserResponse>{ new UserResponse { Email = "aa@aa.aa" },
            new UserResponse { Email = "bb@aa.aa" }, new UserResponse{Email="test@aa.aa"} };

            var testJudges = new List<UserResponse> { new UserResponse { Email = "test@aa.aa" } };

            var result = judgesList.Except(testJudges, UserAccountService.CompareJudgeById).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("aa@aa.aa", result[0].Email);
            Assert.AreEqual("bb@aa.aa", result[1].Email);

        }

        [Test]
        public async Task Should_filter_users()
        {
            var serialised = JsonConvert.SerializeObject(azureAdGraphQueryResponse);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await _service.GetUserByFilterAsync("user@test.aa");

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
            azureAdGraphQueryResponse = new AzureAdGraphQueryResponse<AzureAdGraphUserResponse>();
            var serialised = JsonConvert.SerializeObject(azureAdGraphQueryResponse);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await _service.GetUserByFilterAsync("user@test.aa");

            Assert.IsNull(result);
        }

        [Test]
        public async Task Should_get_group_for_user()
        {
            var serialised = JsonConvert.SerializeObject(_directoryObject);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await _service.GetGroupsForUserAsync("user@test.aa");

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
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await _service.GetGroupsForUserAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_with_no_prefix()
        {
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "adam.green" });
            var result = await _service.CheckForNextAvailableUsernameAsync("Adam","Green");

            Assert.IsTrue(result.Contains("adam.green@hearings.test.server.net"));
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_find_with_prefix()
        {
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "adam.green2@hearings.test.server.net", "adam.green@hearings.test.server.net", "adam.green1@hearings.test.server.net" });
            var result = await _service.CheckForNextAvailableUsernameAsync("Adam", "Green");

            Assert.IsTrue(result.Contains("adam.green3@hearings.test.server.net"));
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_find_null()
        {
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> ());
            var result = await _service.CheckForNextAvailableUsernameAsync("Adam", "Green");

            Assert.IsTrue(result.Contains("adam.green@hearings.test.server.net"));
        }
    }
}
