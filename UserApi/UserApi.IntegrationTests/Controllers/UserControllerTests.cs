using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Faker;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.Services.Models;

namespace UserApi.IntegrationTests.Controllers
{
    public class UserController : ControllerTestsBase
    {
        private readonly AccountEndpoints _accountEndpoints = new ApiUriFactory().AccountEndpoints;
        private readonly UserEndpoints _userEndpoints = new ApiUriFactory().UserEndpoints;
        private string _newUserId;

        [Test]
        public async Task should_create_citizen_user_on_ad()
        {
            var createUserRequest = new CreateUserRequest
            {
                RecoveryEmail = Internet.Email(),
                FirstName = Name.First(),
                LastName = Name.Last()
            };
            var createUserHttpRequest = new StringContent(
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(createUserRequest),
                Encoding.UTF8, "application/json");

            var createUserResponse =
                await SendPostRequestAsync(_userEndpoints.CreateUser, createUserHttpRequest);

            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(createUserResponse.Content
                    .ReadAsStringAsync().Result);
            TestContext.WriteLine($"Response:{ApiRequestHelper.SerialiseRequestToSnakeCaseJson(createUserModel)}");
            createUserModel.Should().NotBeNull();
            createUserModel.UserId.Should().NotBeNullOrEmpty();
            createUserModel.Username.ToLower().Should()
                .Be($@"{createUserRequest.FirstName}.{createUserRequest.LastName}@hearings.reform.hmcts.net".ToLower());
            createUserModel.OneTimePassword.Should().NotBeNullOrEmpty();

            _newUserId = createUserModel.UserId;

            var addSsprGroupRequest = new AddUserToGroupRequest
                {UserId = createUserModel.UserId, GroupName = "SSPR Enabled"};
            var addSsprGroupHttpRequest = new StringContent(
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(addSsprGroupRequest),
                Encoding.UTF8, "application/json");
            var addSsprGroupHttpResponse =
                await SendPatchRequestAsync(_accountEndpoints.AddUserToGroup, addSsprGroupHttpRequest);
            addSsprGroupHttpResponse.IsSuccessStatusCode.Should().BeTrue();

            var addExternalGroupRequest = new AddUserToGroupRequest
                {UserId = createUserModel.UserId, GroupName = "External"};
            var addExternalGroupHttpRequest = new StringContent(
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(addExternalGroupRequest),
                Encoding.UTF8, "application/json");
            var addExternalGroupHttpResponse =
                await SendPatchRequestAsync(_accountEndpoints.AddUserToGroup, addExternalGroupHttpRequest);
            addExternalGroupHttpResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        [Test]
        public async Task should_get_user_by_id()
        {
            const string userId = "60c7fae1-8733-4d82-b912-eece8d55d54c";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserId(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserId.Should().Be(userId);
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task should_get_user_by_id_not_found_with_bogus_user_id()
        {
            var userId = "foo";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserId(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task should_get_user_profile_by_user_principal_name()
        {
            const string username = "VirtualRoomAdministrator@hearings.reform.hmcts.net";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.Email.Should().NotBeNullOrWhiteSpace();
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.LastName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.UserRole.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task should_get_user_profile_by_user_principal_name_not_found_with_bogus_mail()
        {
            const string username = "i.do.not.exist@hearings.reform.hmcts.net";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Test]
        public async Task should_get_user_profile_by_email()
        {
            const string email = "VirtualRoomAdministrator@kinley.com";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.Email.Should().NotBeNullOrWhiteSpace();
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.LastName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.UserRole.Should().NotBeNullOrWhiteSpace();
        }


        [Test]
        public async Task should_get_profile_by_email_not_found_with_bogus_mail()
        {
            const string email = "i.do.not.exist@nowhere.ever.com";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [TearDown]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                    $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
                var result = client.SendAsync(httpRequestMessage).Result;
                result.IsSuccessStatusCode.Should().BeTrue($"{_newUserId} should be deleted");
                _newUserId = null;
            }
        }
    }
}