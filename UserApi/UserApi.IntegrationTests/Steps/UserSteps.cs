using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Requests;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Model.UserRole;
using FluentAssertions;
using Polly;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using Testing.Common.Helpers;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.IntegrationTests.Contexts;
using UserApi.IntegrationTests.Helpers;
using UserApi.Services.Models;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;
using static Testing.Common.Helpers.UserApiUriFactory.UserEndpoints;

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class UserSteps : BaseSteps
    {
        private readonly TestContext _c;

        public UserSteps(TestContext c)
        {
            _c = c;
        }

        [Given(@"I have a new hearings reforms user account request with a (.*) email")]
        [Given(@"I have a new hearings reforms user account request with an (.*) email")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestForTheUser(Scenario scenario)
        {
            _c.Uri = CreateUser;
            _c.HttpMethod = HttpMethod.Post;
            var createUserRequest = new CreateUserRequestBuilder().Build();

            switch (scenario)
            {
                case Scenario.Valid:
                {
                    break;
                }
                case Scenario.Existing:
                {
                    createUserRequest.RecoveryEmail = _c.UserApiConfig.TestSettings.ExistingEmail;
                    createUserRequest.FirstName = _c.UserApiConfig.TestSettings.ExistingUserFirstname;
                    createUserRequest.LastName = _c.UserApiConfig.TestSettings.ExistingUserLastname;
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

            _c.HttpContent = new StringContent(
                RequestHelper.SerialiseRequestToSnakeCaseJson(createUserRequest),
                Encoding.UTF8, "application/json");
        }


        [Given(@"I have a get user by AD user Id request for a (.*) user with (.*)")]
        [Given(@"I have a get user by AD user Id request for an (.*) user with (.*)")]
        public void GivenIHaveAGetUserByAdUserIdRequestForTheUser(Scenario scenario, UserRole userRole)
        {
            _c.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _c.Test.UserRole = userRole;
                    _c.Uri = GetUserByAdUserId(UserManager.GetUserFromRole(_c.UserAccounts, userRole).AlternativeEmail);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _c.Uri = GetUserByAdUserId("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _c.Uri = GetUserByAdUserId(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have a get user by user principal name request for a (.*) user principal name")]
        [Given(@"I have a get user by user principal name request for an (.*) user principal name")]
        public void GivenIHaveAGetUserByUserPrincipalNameRequestForTheUserPrincipalName(Scenario scenario)
        {
            _c.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _c.Uri = GetUserByAdUserName(_c.UserApiConfig.TestSettings.ExistingUserPrincipal);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _c.Uri = GetUserByAdUserName("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _c.Uri = GetUserByAdUserName(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have a new user")]
        public async Task GivenIHaveANewUser()
        {
            _c.Test.NewUser = await CreateTheNewUser();
            await AddUserToExternalGroup(_c.Test.NewUser.UserId);
        }

        private async Task<NewUserResponse> CreateTheNewUser()
        {
            _c.Uri = CreateUser;
            _c.HttpMethod = HttpMethod.Post;
            var createUserRequest = new CreateUserRequestBuilder().Build();
            _c.HttpContent = new StringContent(
                RequestHelper.SerialiseRequestToSnakeCaseJson(createUserRequest),
                Encoding.UTF8, "application/json");
            _c.ResponseMessage = await SendPostRequestAsync(_c);
            _c.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await _c.ResponseMessage.Content.ReadAsStringAsync();
            return RequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(json);
        }

        private async Task AddUserToExternalGroup(string userId)
        {
            _c.HttpMethod = HttpMethod.Patch;
            _c.Uri = AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = userId,
                GroupName = "External"
            };
            var jsonBody = RequestHelper.SerialiseRequestToSnakeCaseJson(addUserRequest);
            _c.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            _c.ResponseMessage = await SendPatchRequestAsync(_c);
            _c.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Given(@"I have a delete user request for the new user")]
        public void GivenIHaveADeleteUserRequestForTheNewUser()
        {
            _c.HttpMethod = HttpMethod.Delete;
            _c.Uri = DeleteUser(_c.Test.NewUser.Username);
        }

        [Given(@"I have a delete user request for a nonexistent user")]
        public void GivenIHaveADeleteUserRequest()
        {
            _c.HttpMethod = HttpMethod.Delete;
            _c.Uri = DeleteUser("Does not exist");
        }

        [Given(@"I have a get user profile by email request for a (.*) email")]
        [Given(@"I have a get user profile by email request for an (.*) email")]
        public void GivenIHaveAGetUserProfileByEmailRequestForTheEmail(Scenario scenario)
        {
            _c.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _c.Uri = GetUserByEmail(_c.UserApiConfig.TestSettings.ExistingEmail);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _c.Uri = GetUserByEmail("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _c.Uri = GetUserByEmail(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [When(@"I send the delete request to the endpoint with polling")]
        public async Task WhenISendTheDeleteRequestToTheEndpointWithPolling()
        {
            _c.ResponseMessage = new HttpResponseMessage();

            var policy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (msg, time) => { Console.WriteLine($"Received {msg.Result.StatusCode} for deleting user, retrying..."); });

            var getResponse = await policy.ExecuteAsync
            (
                async () => await SendDeleteRequestAsync(_c)
            );

            getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _c.ResponseMessage = getResponse;
        }

        [Then(@"the user should be added")]
        public async Task ThenTheUserShouldBeAdded()
        {
            var json = await _c.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(json);
            model.Should().NotBeNull();
            model.OneTimePassword.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.Username.Should().NotBeNullOrEmpty();
            _c.Test.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public async Task ThenTheUserDetailsShouldBeRetrieved()
        {
            var json = await _c.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(json);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.FirstName.Should().NotBeNullOrEmpty();
            model.LastName.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.UserName.Should().NotBeNullOrEmpty();
            if(_c.Test.UserRole == UserRole.Individual || _c.Test.UserRole == UserRole.Representative)
            {
                model.Email.Should().NotBeNullOrEmpty();
            }

            model.UserRole.Should().Be(_c.Test.UserRole == UserRole.VideoHearingsOfficer
                ? UserRole.VideoHearingsOfficer.ToString()
                : _c.Test.UserRole.ToString());
        }

        [Then(@"the response should be empty")]
        public async Task ThenTheResponseShouldBeEmpty()
        {
            var json = await _c.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(json);
            model.Should().BeNull();
        }

        [AfterScenario]
        public async Task ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_c.Test.NewUserId)) return;
            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(_c.Test.NewUserId, _c.GraphApiToken);
            _c.Test.NewUserId = null;
        }
    }
}
