using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using Faker;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common.Configuration;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;
using static Testing.Common.Helpers.UserApiUriFactory.UserEndpoints;

namespace UserApi.IntegrationTests.Controllers
{
    public class UserController : ControllerTestsBase
    {
        private string _newUserId;

        [Test]
        public async Task Should_create_citizen_user_on_ad()
        {
            var createUserRequest = new CreateUserRequest
            {
                RecoveryEmail = $"Automation_{Name.First()}@hmcts.net",
                FirstName = $"Automation_{Name.First()}",
                LastName = $"Automation_{Name.Last()}"
            };
            var createUserHttpRequest = new StringContent(
                RequestHelper.Serialise(createUserRequest),
                Encoding.UTF8, "application/json");

            var createUserResponse =
                await SendPostRequestAsync(CreateUser, createUserHttpRequest);

            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel =
                RequestHelper.Deserialise<NewUserResponse>(createUserResponse.Content
                    .ReadAsStringAsync().Result);
            TestContext.WriteLine($"Response:{RequestHelper.Serialise(createUserModel)}");
            createUserModel.Should().NotBeNull();
            createUserModel.UserId.Should().NotBeNullOrEmpty();
            createUserModel.Username.ToLower().Should()
                .Be($@"{createUserRequest.FirstName}.{createUserRequest.LastName}@{TestConfig.Instance.Settings.ReformEmail}".ToLower());
            createUserModel.OneTimePassword.Should().NotBeNullOrEmpty();

            _newUserId = createUserModel.UserId;

            var addExternalGroupRequest = new AddUserToGroupRequest
                {UserId = createUserModel.UserId, GroupName = TestConfig.Instance.Settings.AdGroup.External};
            var addExternalGroupHttpRequest = new StringContent(
                RequestHelper.Serialise(addExternalGroupRequest),
                Encoding.UTF8, "application/json");
            var addExternalGroupHttpResponse =
                await SendPatchRequestAsync(AddUserToGroup, addExternalGroupHttpRequest);
            addExternalGroupHttpResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        [Test]
        public async Task Should_return_objectconflict_when_crate_user_with_email_for_which_account_exists()
        {
            var email = $"Automation_{Name.First()}@hmcts.net";
            var createUserModel = await CreateNewUser(email);
            TestContext.WriteLine($"Response:{RequestHelper.Serialise(createUserModel)}");
            createUserModel.Should().NotBeNull();
            createUserModel.UserId.Should().NotBeNullOrEmpty();
            createUserModel.OneTimePassword.Should().NotBeNullOrEmpty();

            _newUserId = createUserModel.UserId;
            var responseMessage = await CreateAdUser(email);
            responseMessage.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Test]
        public async Task Should_get_user_by_id()
        {
            string userId = TestConfig.Instance.TestSettings.ExistingUserId;
            var getResponse = await SendGetRequestAsync(GetUserByAdUserId(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                RequestHelper.Deserialise<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserId.Should().Be(userId);
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.DisplayName.Should().NotBeNullOrWhiteSpace();
        }
        
        [Test]
        public async Task Should_get_case_administrator_by_id()
        {
            var username = $"Automation01Administrator01@{TestConfig.Instance.Settings.ReformEmail}";
            var getResponse = await SendGetRequestAsync(GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                RequestHelper.Deserialise<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.FirstName.Should().Be("Automation01");
            userResponseModel.LastName.Should().Be("Administrator01");
            userResponseModel.DisplayName.Should().Be("Automation01 Administrator01");
        }

        [Test]
        public async Task Should_get_user_by_id_not_found_with_bogus_user_id()
        {
            const string userId = "foo";
            var getResponse = await SendGetRequestAsync(GetUserByAdUserId(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_user_profile_by_user_principal_name()
        {
            var username = $"Automation01Administrator01@{TestConfig.Instance.Settings.ReformEmail}";
            var getResponse = await SendGetRequestAsync(GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel =
                RequestHelper.Deserialise<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.Email.Should().NotBeNullOrWhiteSpace();
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.LastName.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_get_user_profile_by_user_principal_name_not_found_with_bogus_mail()
        {
            var username = $"i.do.not.exist@{TestConfig.Instance.Settings.ReformEmail}";
            var getResponse = await SendGetRequestAsync(GetUserByAdUserName(username));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Test]
        public async Task Should_get_user_profile_by_email()
        {
            var email = $"Admin.Kinly@{TestConfig.Instance.Settings.ReformEmail}";
            var getResponse = await SendGetRequestAsync(GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel = RequestHelper.Deserialise<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.Email.Should().NotBeNullOrWhiteSpace();
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.LastName.Should().NotBeNullOrWhiteSpace();
        }


        [Test]
        public async Task Should_get_profile_by_email_not_found_with_bogus_mail()
        {
            const string email = "i.do.not.exist@hmcts.net";
            var getResponse = await SendGetRequestAsync(GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_judges()
        {
            var getResponse = await SendGetRequestAsync(GetJudges());
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var usersForGroupModel = RequestHelper.Deserialise<List<UserResponse>>(getResponse.Content.ReadAsStringAsync().Result);
            usersForGroupModel.Should().NotBeEmpty();

            var testAccount = usersForGroupModel.First(u => u.Email.ToLower().EndsWith($".judge@{TestConfig.Instance.Settings.ReformEmail}".ToLower()));
            testAccount.Email.Should().NotBeNullOrWhiteSpace();
            testAccount.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_refresh_judges_cache()
        {
            var response = await SendGetRequestAsync(RefreshJudgesCache());
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        
        [Test]
        public async Task Should_delete_user()
        {
            // Create User
            var createUserResponse = await CreateAdUser($"Automation_{Name.First()}@hmcts.net");
            
            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel = RequestHelper.Deserialise<NewUserResponse>
            (
                createUserResponse.Content.ReadAsStringAsync().Result
            );
            
            //Add User to group
            var addExternalGroupHttpRequest = new StringContent
            (
                RequestHelper.Serialise(new AddUserToGroupRequest
                {
                    UserId = createUserModel.UserId, GroupName = TestConfig.Instance.Settings.AdGroup.External
                }),
                Encoding.UTF8, "application/json"
            );
            
            var addExternalGroupHttpResponse = await SendPatchRequestAsync(AddUserToGroup, addExternalGroupHttpRequest);
            addExternalGroupHttpResponse.IsSuccessStatusCode.Should().BeTrue();
            
            // Delete User
            var result = await SendDeleteRequestAsync(DeleteUser(createUserModel.Username));
            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Test]
        public async Task should_return_bad_request_when_updating_nonexistent_user_with_missing_name()
        {
            var userId = Guid.NewGuid();
            var updateUserRequest = new UpdateUserAccountRequest
            {
                LastName = "Doe"
            };
            var jsonBody = RequestHelper.Serialise(updateUserRequest);
            var stringContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var result = await SendPatchRequestAsync(UpdateUserAccount(userId), stringContent);
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Test]
        public async Task should_return_not_found_when_updating_nonexistent_user()
        {
            var userId = Guid.NewGuid();
            var updateUserRequest = new UpdateUserAccountRequest
            {
                FirstName = "John",
                LastName = "Doe"
            };
            var jsonBody = RequestHelper.Serialise(updateUserRequest);
            var stringContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var result = await SendPatchRequestAsync(UpdateUserAccount(userId), stringContent);
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task should_return_ok_and_updated_user_when_updating_an_account_successfully()
        {
            var existingUser = await CreateNewUser($"Automation_{Name.First()}@hmcts.net");
            var userId = Guid.Parse(existingUser.UserId);
            var username = existingUser.Username;
            var updateUserRequest = new UpdateUserAccountRequest
            {
                FirstName = "RandomTest",
                LastName = "UpdatedTest"
            };
            var jsonBody = RequestHelper.Serialise(updateUserRequest);
            var stringContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            var responseMessage = await SendPatchRequestAsync(UpdateUserAccount(userId), stringContent);
            
            responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedUserResponse = RequestHelper.Deserialise<UserResponse>(await responseMessage.Content.ReadAsStringAsync());
            updatedUserResponse.FirstName.Should().Be(updateUserRequest.FirstName);
            updatedUserResponse.LastName.Should().Be(updateUserRequest.LastName);
            updatedUserResponse.Email.Should().NotBe(username);
        }

        [Test]
        public async Task Should_get_user_by_email_containing_slash()
        {
            var email = "Automation02/Individual01@hmcts.net";
            var getResponse = await SendGetRequestAsync(GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel = RequestHelper.Deserialise<UserProfile>(getResponse.Content
                .ReadAsStringAsync().Result);
            userResponseModel.UserName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.Email.Should().NotBeNullOrWhiteSpace();
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.LastName.Should().NotBeNullOrWhiteSpace();
        }
        
        [TearDown]
        public async Task ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            TestContext.WriteLine($"Attempting to delete account {_newUserId}");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
            await client.SendAsync(httpRequestMessage);
            _newUserId = null;
        }

        private async Task<HttpResponseMessage> CreateAdUser(string email)
        {
            return await SendPostRequestAsync
            (
                CreateUser,
                new StringContent
                (
                    RequestHelper.Serialise(new CreateUserRequest
                    {
                        RecoveryEmail = email,
                        FirstName = $"Automation_{Name.First()}",
                        LastName = $"Automation_{Name.Last()}"
                    }),
                    Encoding.UTF8, "application/json"
                )
            );
        }

        private async Task<NewUserResponse> CreateNewUser(string email)
        {
            var createUserResponse = await CreateAdUser(email);
            createUserResponse.IsSuccessStatusCode.Should().BeTrue();
            
            var createUserModel = RequestHelper.Deserialise<NewUserResponse>
            (
                createUserResponse.Content.ReadAsStringAsync().Result
            );
            _newUserId = createUserModel.UserId;
            TestContext.WriteLine($"Created account {_newUserId}");
            return createUserModel;
        }
    }
}
