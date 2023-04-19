using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Model.UserRole;
using FluentAssertions;
using Polly;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using Testing.Common.Configuration;
using Testing.Common.Helpers;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.IntegrationTests.Contexts;
using UserApi.IntegrationTests.Helpers;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;
using static Testing.Common.Helpers.UserApiUriFactory.UserEndpoints;

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class UserSteps : BaseSteps
    {
        private readonly TestContext _testContext;
        private NewUserResponse _newUser;

        public UserSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"I have a new hearings reforms user account request with a (.*) email")]
        [Given(@"I have a new hearings reforms user account request with an (.*) email")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestForTheUser(Scenario scenario)
        {
            _testContext.Uri = CreateUser;
            _testContext.HttpMethod = HttpMethod.Post;
            var createUserRequest = new CreateUserRequestBuilder().Build();

            switch (scenario)
            {
                case Scenario.Valid:
                {
                    break;
                }
                case Scenario.Existing:
                {
                    createUserRequest.RecoveryEmail = _testContext.Config.TestSettings.ExistingEmail;
                    createUserRequest.FirstName = _testContext.Config.TestSettings.ExistingUserFirstname;
                    createUserRequest.LastName = _testContext.Config.TestSettings.ExistingUserLastname;
                    break;
                }
                case Scenario.Invalid:
                {
                    createUserRequest.RecoveryEmail = "";
                    createUserRequest.FirstName = "";
                    createUserRequest.LastName = "";
                    break;
                }
                case Scenario.IncorrectFormat:
                {
                    createUserRequest.RecoveryEmail = "EmailWithoutAnAtSymbol";
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }

            _testContext.HttpContent = new StringContent(
                RequestHelper.Serialise(createUserRequest),
                Encoding.UTF8, "application/json");
        }


        [Given(@"I have a get user by AD user Id request for a (.*) user with (.*)")]
        [Given(@"I have a get user by AD user Id request for an (.*) user with (.*)")]
        public void GivenIHaveAGetUserByAdUserIdRequestForTheUser(Scenario scenario, UserRole userRole)
        {
            _testContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _testContext.Uri = GetUserByAdUserId(GetExistingUserIdForRole(userRole));
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _testContext.Uri = GetUserByAdUserId("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _testContext.Uri = GetUserByAdUserId(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }
        private string GetExistingUserIdForRole(UserRole userRole)
        {
            return UserManager.GetUserFromRole(_testContext.UserAccounts, userRole).Username;
        }

        [Given(@"I have a get user by user principal name request for a (.*) user principal name")]
        [Given(@"I have a get user by user principal name request for an (.*) user principal name")]
        public void GivenIHaveAGetUserByUserPrincipalNameRequestForTheUserPrincipalName(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _testContext.Uri = GetUserByAdUserName(_testContext.Config.TestSettings.ExistingUserPrincipal);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _testContext.Uri = GetUserByAdUserName("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _testContext.Uri = GetUserByAdUserName(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have a new user")]
        public async Task GivenIHaveANewUser()
        {
            _newUser = await CreateTheNewUser();
            await AddUserToExternalGroup(_newUser.UserId);
        }

        private async Task<NewUserResponse> CreateTheNewUser()
        {
            _testContext.Uri = CreateUser;
            _testContext.HttpMethod = HttpMethod.Post;
            var createUserRequest = new CreateUserRequestBuilder().Build();
            _testContext.HttpContent = new StringContent(
                RequestHelper.Serialise(createUserRequest),
                Encoding.UTF8, "application/json");
            _testContext.ResponseMessage = await SendPostRequestAsync(_testContext);
            _testContext.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await _testContext.ResponseMessage.Content.ReadAsStringAsync();
            return RequestHelper.Deserialise<NewUserResponse>(json);
        }

        private async Task AddUserToExternalGroup(string userId)
        {
            _testContext.HttpMethod = HttpMethod.Patch;
            _testContext.Uri = AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = userId,
                GroupName = TestConfig.Instance.Settings.AdGroup.External
            };
            var jsonBody = RequestHelper.Serialise(addUserRequest);
            _testContext.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            _testContext.ResponseMessage = await SendPatchRequestAsync(_testContext);
            _testContext.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Given(@"I have a delete user request for the new user")]
        public void GivenIHaveADeleteUserRequestForTheNewUser()
        {
            _testContext.HttpMethod = HttpMethod.Delete;
            _testContext.Uri = DeleteUser(_newUser.Username);
        }

        [Given(@"I have a delete user request for a nonexistent user")]
        public void GivenIHaveADeleteUserRequest()
        {
            _testContext.HttpMethod = HttpMethod.Delete;
            _testContext.Uri = DeleteUser("Does not exist");
        }

        [Given(@"I have a get user profile by email request for a (.*) email")]
        [Given(@"I have a get user profile by email request for an (.*) email")]
        public void GivenIHaveAGetUserProfileByEmailRequestForTheEmail(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _testContext.Uri = GetUserByEmail(_testContext.Config.TestSettings.ExistingEmail);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _testContext.Uri = GetUserByEmail("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _testContext.Uri = GetUserByEmail(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [When(@"I send the delete request to the endpoint with polling")]
        public async Task WhenISendTheDeleteRequestToTheEndpointWithPolling()
        {
            _testContext.ResponseMessage = new HttpResponseMessage();

            var policy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (msg, time) => { Console.WriteLine($"Received {msg.Result.StatusCode} for deleting user, retrying..."); });

            var getResponse = await policy.ExecuteAsync
            (
                async () => await SendDeleteRequestAsync(_testContext)
            );

            getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _testContext.ResponseMessage = getResponse;
        }

        [Then(@"the user should be added")]
        public async Task ThenTheUserShouldBeAdded()
        {
            var json = await _testContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.Deserialise<NewUserResponse>(json);
            model.Should().NotBeNull();
            model.OneTimePassword.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.Username.Should().NotBeNullOrEmpty();
            _testContext.Test.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public async Task ThenTheUserDetailsShouldBeRetrieved()
        {
            var json = await _testContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.Deserialise<UserProfile>(json);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.FirstName.Should().NotBeNullOrEmpty();
            model.LastName.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.UserName.Should().NotBeNullOrEmpty();
        }

        [Then(@"the response should be empty")]
        public async Task ThenTheResponseShouldBeEmpty()
        {
            var json = await _testContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.Deserialise<UserProfile>(json);
            model.Should().BeNull();
        }

        [AfterScenario]
        public async Task ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_testContext.Test.NewUserId)) return;
            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(_testContext.Test.NewUserId, _testContext.Tokens.GraphApiBearerToken);
            _testContext.Test.NewUserId = null;
        }
    }
}
