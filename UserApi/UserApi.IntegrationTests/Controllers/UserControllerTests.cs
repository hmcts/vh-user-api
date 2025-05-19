using System.Net;
using System.Net.Http.Headers;
using Bogus;
using Bogus.DataSets;
using NUnit.Framework;
using Polly;
using Testing.Common.Configuration;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.UserEndpoints;

namespace UserApi.IntegrationTests.Controllers
{
    public class UserController : ControllerTestsBase
    {
        private string _newUserId;
        private readonly Name _name = new Faker().Name;

        [Test]
        public async Task Should_create_citizen_user_on_ad()
        {
            var createUserRequest = new CreateUserRequest
            {
                RecoveryEmail = $"Automation_{_name.FirstName()}@hmcts.net",
                FirstName = $"Automation_{_name.FirstName()}",
                LastName = $"Automation_{_name.LastName()}"
            };
            var createUserHttpRequest = new StringContent(
                ApiRequestHelper.Serialise(createUserRequest),
                Encoding.UTF8, "application/json");

            var createUserResponse = await SendPostRequestAsync(CreateUser, createUserHttpRequest);
            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel = ApiRequestHelper.Deserialise<NewUserResponse>(createUserResponse.Content.ReadAsStringAsync().Result);
            TestContext.WriteLine($"Response:{ApiRequestHelper.Serialise(createUserModel)}");
            createUserModel.Should().NotBeNull();
            createUserModel.UserId.Should().NotBeNullOrEmpty();
            _newUserId = createUserModel.UserId;
            createUserModel.Username.ToLower().Should()
                .Be($@"{createUserRequest.FirstName}.{createUserRequest.LastName}@{TestConfig.Instance.Settings.ReformEmail}".ToLower());
            createUserModel.OneTimePassword.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Should_return_object_conflict_when_create_user_with_email_for_which_account_exists()
        {
            var email = $"Automation_{_name.FirstName()}@hmcts.net";
            var createUserModel = await CreateNewUser(email);
            TestContext.WriteLine($"Response:{ApiRequestHelper.Serialise(createUserModel)}");
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
            var userResponseModel = ApiRequestHelper.Deserialise<UserProfile>(getResponse.Content
                    .ReadAsStringAsync().Result);
            userResponseModel.UserId.Should().Be(userId);
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.DisplayName.Should().NotBeNullOrWhiteSpace();
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
                ApiRequestHelper.Deserialise<UserProfile>(getResponse.Content
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
            var userResponseModel = ApiRequestHelper.Deserialise<UserProfile>(getResponse.Content
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
            const string email = "i.do.not.exist@hmcts.net";
            var getResponse = await SendGetRequestAsync(GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_none_user_role_for_user_not_in_group()
        {
            // Create User
            var createUserResponse = await CreateAdUser($"Automation_{_name.FirstName()}@hmcts.net");
            
            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel = ApiRequestHelper.Deserialise<NewUserResponse>
            (
                createUserResponse.Content.ReadAsStringAsync().Result
            );

            const int retries = 5;

            var policy = Policy
                .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                .WaitAndRetryAsync(retries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            
            var getResponse = await policy.ExecuteAsync(async () => await SendGetRequestAsync(GetUserByAdUserName(createUserModel.Username)));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel = ApiRequestHelper.Deserialise<UserProfile>(await getResponse.Content.ReadAsStringAsync());
            userResponseModel.UserRole.Should().Be("None");

            // Delete User
            var result = await SendDeleteRequestAsync(DeleteUser(createUserModel.Username));
            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        
        [Test]
        public async Task Should_delete_user()
        {
            // Create User
            var createUserResponse = await CreateAdUser($"Automation_{_name.FirstName()}@hmcts.net");
            createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createUserModel = ApiRequestHelper.Deserialise<NewUserResponse>
            (
                createUserResponse.Content.ReadAsStringAsync().Result
            );
            
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
            var jsonBody = ApiRequestHelper.Serialise(updateUserRequest);
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
            var jsonBody = ApiRequestHelper.Serialise(updateUserRequest);
            var stringContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var result = await SendPatchRequestAsync(UpdateUserAccount(userId), stringContent);
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task should_return_ok_and_updated_user_when_updating_an_account_successfully()
        {
            var existingUser = await CreateNewUser($"Automation_{_name.FirstName()}@hmcts.net");
            _newUserId = existingUser.UserId;
            var userId = Guid.Parse(existingUser.UserId);
            var username = existingUser.Username;
            var updateUserRequest = new UpdateUserAccountRequest
            {
                FirstName = "RandomTest",
                LastName = "UpdatedTest"
            };
            var jsonBody = ApiRequestHelper.Serialise(updateUserRequest);
            var stringContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            var responseMessage = await SendPatchRequestAsync(UpdateUserAccount(userId), stringContent);
            
            responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedUserResponse = ApiRequestHelper.Deserialise<UserResponse>(await responseMessage.Content.ReadAsStringAsync());
            updatedUserResponse.FirstName.Should().Be(updateUserRequest.FirstName);
            updatedUserResponse.LastName.Should().Be(updateUserRequest.LastName);
            updatedUserResponse.Email.Should().NotBe(username);
        }

        [Test]
        public async Task should_return_ok_and_updated_user_when_updating_an_account_contact_email_successfully()
        {
            var newUser = await CreateNewUser($"Automation_{_name.FirstName()}@hmcts.net");
            _newUserId = newUser.UserId;
            var existingUser = await GetUser(newUser.UserId);
            var userId = Guid.Parse(existingUser.UserId);
            var updateUserRequest = new UpdateUserAccountRequest
            {
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                ContactEmail = "newEmail@email.com"
            };
            var jsonBody = ApiRequestHelper.Serialise(updateUserRequest);
            var stringContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            var responseMessage = await SendPatchRequestAsync(UpdateUserAccount(userId), stringContent);
            
            responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedUserResponse = ApiRequestHelper.Deserialise<UserResponse>(await responseMessage.Content.ReadAsStringAsync());
            updatedUserResponse.FirstName.Should().Be(updateUserRequest.FirstName);
            updatedUserResponse.LastName.Should().Be(updateUserRequest.LastName);
            updatedUserResponse.ContactEmail.Should().Be(updateUserRequest.ContactEmail);
        }

        [Test]
        public async Task Should_get_user_by_email_containing_slash()
        {
            var email = "Automation02/Individual01@hmcts.net";
            var getResponse = await SendGetRequestAsync(GetUserByEmail(email));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userResponseModel = ApiRequestHelper.Deserialise<UserProfile>(getResponse.Content
                .ReadAsStringAsync().Result);
            userResponseModel.UserName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.Email.Should().NotBeNullOrWhiteSpace();
            userResponseModel.FirstName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.LastName.Should().NotBeNullOrWhiteSpace();
            userResponseModel.UserRole.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_get_judges_by_user_name()
        {
            var getResponse = await SendGetRequestAsync(GetJudgesByUsername());
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var judges = ApiRequestHelper.Deserialise<IEnumerable<UserResponse>>(getResponse.Content.ReadAsStringAsync().Result);
            judges.Count().Should().BeGreaterThan(0);
        }
       
        [TearDown]
        public async Task ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            TestContext.WriteLine($"Attempting to delete account {_newUserId}");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
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
                    ApiRequestHelper.Serialise(new CreateUserRequest
                    {
                        RecoveryEmail = email,
                        FirstName = $"Automation_{_name.FirstName()}",
                        LastName = $"Automation_{_name.LastName()}"
                    }),
                    Encoding.UTF8, "application/json"
                )
            );
        }

        private async Task<NewUserResponse> CreateNewUser(string email)
        {
            var createUserResponse = await CreateAdUser(email);
            createUserResponse.IsSuccessStatusCode.Should().BeTrue();
            
            var createUserModel = ApiRequestHelper.Deserialise<NewUserResponse>
            (
                createUserResponse.Content.ReadAsStringAsync().Result
            );
            _newUserId = createUserModel.UserId;
            TestContext.WriteLine($"Created account {_newUserId}");
            return createUserModel;
        }
        
        private async Task<UserProfile> GetUser(string userId)
        {
            var getResponse = await SendGetRequestAsync(GetUserByAdUserId(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            return ApiRequestHelper.Deserialise<UserProfile>(await getResponse.Content.ReadAsStringAsync());
        }
    }
}
