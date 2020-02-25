using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Faker;
using FluentAssertions;
using NUnit.Framework;
using Polly;
using Testing.Common;
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
        public async Task Should_create_citizen_user_on_ad()
        {
            var createUserRequest = new CreateUserRequest
            {
                RecoveryEmail = $"Automation_{Internet.Email()}",
                FirstName = $"Automation_{Name.First()}",
                LastName = $"Automation_{Name.Last()}"
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
                .Be($@"{createUserRequest.FirstName}.{createUserRequest.LastName}@{TestConfig.Instance.TestSettings.ReformEmail}".ToLower());
            createUserModel.OneTimePassword.Should().NotBeNullOrEmpty();

            _newUserId = createUserModel.UserId;

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
        public async Task Should_get_user_by_id()
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
        public async Task Should_get_case_administrator_by_id()
        {
            var username = $"Automation01Administrator01@{TestConfig.Instance.TestSettings.ReformEmail}";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserRole.Should().Be("CaseAdmin");
            userResponseModel.FirstName.Should().Be("Automation01");
            userResponseModel.LastName.Should().Be("Administrator01");
            userResponseModel.DisplayName.Should().Be("Automation01 Administrator01");
        }

        [Test]
        public async Task Should_get_user_by_id_not_found_with_bogus_user_id()
        {
            var userId = "foo";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserId(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_user_profile_by_user_principal_name()
        {
            var username = $"Automation01Administrator01@{TestConfig.Instance.TestSettings.ReformEmail}";
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
        public async Task Should_get_user_profile_by_user_principal_name_not_found_with_bogus_mail()
        {
            var username = $"i.do.not.exist@{TestConfig.Instance.TestSettings.ReformEmail}";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Test]
        public async Task Should_get_user_profile_by_email()
        {
            var email = $"Admin.Kinly@{TestConfig.Instance.TestSettings.ReformEmail}";
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
        public async Task Should_get_profile_by_email_not_found_with_bogus_mail()
        {
            const string email = "i.do.not.exist@nowhere.ever.com";
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_users_for_group()
        {
            var getResponse = await SendGetRequestAsync(_userEndpoints.GetJudges());
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var usersForGroupModel = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<UserResponse>>(getResponse.Content.ReadAsStringAsync().Result);
            usersForGroupModel.Should().NotBeEmpty();

            var expectedJudgeUser = usersForGroupModel.FirstOrDefault(u => u.Email == $"Judge.Bever@{TestConfig.Instance.TestSettings.ReformEmail}");
            expectedJudgeUser.DisplayName.Should().Be("Judge Bever");
        }
        
        [Test]
        public async Task Should_delete_user()
        {
            // Create User
            var createUserResponse = await SendPostRequestAsync
            (
                _userEndpoints.CreateUser, 
                new StringContent
                (
                    ApiRequestHelper.SerialiseRequestToSnakeCaseJson(new CreateUserRequest
                    {
                        RecoveryEmail = $"Automation_{Internet.Email()}",
                        FirstName = $"Automation_{Name.First()}",
                        LastName = $"Automation_{Name.Last()}"
                    }),
                    Encoding.UTF8, "application/json"
                )
            );
            
            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>
            (
                createUserResponse.Content.ReadAsStringAsync().Result
            );
            
            //Add User to group

            var addExternalGroupHttpRequest = new StringContent
            (
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(new AddUserToGroupRequest
                {
                    UserId = createUserModel.UserId, GroupName = "External"
                }),
                Encoding.UTF8, "application/json"
            );
            
            var addExternalGroupHttpResponse = await SendPatchRequestAsync(_accountEndpoints.AddUserToGroup, addExternalGroupHttpRequest);
            addExternalGroupHttpResponse.IsSuccessStatusCode.Should().BeTrue();
            
            // Delete User
            var policy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (msg, time) => { Console.WriteLine($"Received {msg.Result.StatusCode} for deleting user, retrying..."); });
           
            var getResponse = await policy.ExecuteAsync
            (
                async () => await SendDeleteRequestAsync(_userEndpoints.DeleteUser(createUserModel.Username))
            );
            
            getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [TearDown]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
            var result = client.SendAsync(httpRequestMessage).Result;
            result.IsSuccessStatusCode.Should().BeTrue($"{_newUserId} should be deleted");
            _newUserId = null;
        }
    }
}