using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class UserSteps : BaseSteps
    {
        private readonly ApiTestContext _apiTestContext;
        private readonly AccountEndpoints _accountEndpoints = new ApiUriFactory().AccountEndpoints;
        private readonly UserEndpoints _endpoints = new ApiUriFactory().UserEndpoints;
        private UserRole? _userRole;
        private NewUserResponse _newUser;

        public UserSteps(ApiTestContext apiTestContext)
        {
            _apiTestContext = apiTestContext;
        }

        [Given(@"I have a new hearings reforms user account request with a (.*) email")]
        [Given(@"I have a new hearings reforms user account request with an (.*) email")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestForTheUser(Scenario scenario)
        {
            _apiTestContext.Uri = _endpoints.CreateUser;
            _apiTestContext.HttpMethod = HttpMethod.Post;
            var createUserRequest = new CreateUserRequestBuilder().Build();

            switch (scenario)
            {
                case Scenario.Valid:
                {
                    break;
                }
                case Scenario.Existing:
                {
                    createUserRequest.RecoveryEmail = _apiTestContext.TestSettings.ExistingEmail;
                    createUserRequest.FirstName = _apiTestContext.TestSettings.ExistingUserFirstname;
                    createUserRequest.LastName = _apiTestContext.TestSettings.ExistingUserLastname;
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

            _apiTestContext.HttpContent = new StringContent(
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(createUserRequest),
                Encoding.UTF8, "application/json");
        }


        [Given(@"I have a get user by AD user Id request for a (.*) user with (.*)")]
        [Given(@"I have a get user by AD user Id request for an (.*) user with (.*)")]
        public void GivenIHaveAGetUserByAdUserIdRequestForTheUser(Scenario scenario, UserRole userRole)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _userRole = userRole;
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId(GetExistingUserIdForRole(userRole));
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }
        private string GetExistingUserIdForRole(UserRole userRole)
        {
            switch (userRole)
            {
                case UserRole.Individual:
                    return _apiTestContext.TestSettings.Individual;
                case UserRole.Representative:
                    return _apiTestContext.TestSettings.Representative;
                case UserRole.VhOfficer:
                    return _apiTestContext.TestSettings.VhOfficer;
                case UserRole.CaseAdmin:
                    return _apiTestContext.TestSettings.CaseAdmin;
                case UserRole.Judge:
                    return _apiTestContext.TestSettings.Judge;
                case UserRole.VhOfficerCaseAdmin:
                    return _apiTestContext.TestSettings.VhOfficerCaseAdmin;
                default:
                    throw new ArgumentException($"Cannot determine type of user role {nameof(userRole)}");
            }
        }

        [Given(@"I have a get user by user principal name request for a (.*) user principal name")]
        [Given(@"I have a get user by user principal name request for an (.*) user principal name")]
        public void GivenIHaveAGetUserByUserPrincipalNameRequestForTheUserPrincipalName(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserName(_apiTestContext.TestSettings.ExistingUserPrincipal);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserName("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserName(" ");
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
            _apiTestContext.Uri = _endpoints.CreateUser;
            _apiTestContext.HttpMethod = HttpMethod.Post;
            var createUserRequest = new CreateUserRequestBuilder().Build();
            _apiTestContext.HttpContent = new StringContent(
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(createUserRequest),
                Encoding.UTF8, "application/json");
            _apiTestContext.ResponseMessage = await SendPostRequestAsync(_apiTestContext);
            _apiTestContext.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            return ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(json);
        }

        private async Task AddUserToExternalGroup(string userId)
        {
            _apiTestContext.HttpMethod = HttpMethod.Patch;
            _apiTestContext.Uri = _accountEndpoints.AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = userId,
                GroupName = "External"
            };
            var jsonBody = ApiRequestHelper.SerialiseRequestToSnakeCaseJson(addUserRequest);
            _apiTestContext.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            _apiTestContext.ResponseMessage = await SendPatchRequestAsync(_apiTestContext);
            _apiTestContext.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Given(@"I have a delete user request for the new user")]
        public void GivenIHaveADeleteUserRequestForTheNewUser()
        {
            _apiTestContext.HttpMethod = HttpMethod.Delete;
            _apiTestContext.Uri = _endpoints.DeleteUser(_newUser.Username);
        }

        [Given(@"I have a delete user request for a nonexistent user")]
        public void GivenIHaveADeleteUserRequest()
        {
            _apiTestContext.HttpMethod = HttpMethod.Delete;
            _apiTestContext.Uri = _endpoints.DeleteUser("Does not exist");
        }

        [Given(@"I have a get user profile by email request for a (.*) email")]
        [Given(@"I have a get user profile by email request for an (.*) email")]
        public void GivenIHaveAGetUserProfileByEmailRequestForTheEmail(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByEmail(_apiTestContext.TestSettings.ExistingEmail);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByEmail("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByEmail(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [When(@"I send the delete request to the endpoint with polling")]
        public async Task WhenISendTheDeleteRequestToTheEndpointWithPolling()
        {
            _apiTestContext.ResponseMessage = new HttpResponseMessage();

            var policy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (msg, time) => { Console.WriteLine($"Received {msg.Result.StatusCode} for deleting user, retrying..."); });

            var getResponse = await policy.ExecuteAsync
            (
                async () => await SendDeleteRequestAsync(_apiTestContext)
            );

            getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _apiTestContext.ResponseMessage = getResponse;
        }

        [Then(@"the user should be added")]
        public async Task ThenTheUserShouldBeAdded()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(json);
            model.Should().NotBeNull();
            model.OneTimePassword.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.Username.Should().NotBeNullOrEmpty();
            _apiTestContext.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public async Task ThenTheUserDetailsShouldBeRetrieved()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(json);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.FirstName.Should().NotBeNullOrEmpty();
            model.LastName.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.UserName.Should().NotBeNullOrEmpty();
            if(_userRole != null)
            {
                if(_userRole == UserRole.Individual || _userRole == UserRole.Representative)
                {
                    model.Email.Should().NotBeNullOrEmpty();
                }

                model.UserRole.Should().Be(_userRole == UserRole.VhOfficerCaseAdmin
                    ? UserRole.VhOfficer.ToString()
                    : _userRole.ToString());
            }
        }

        [Then(@"the response should be empty")]
        public async Task ThenTheResponseShouldBeEmpty()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(json);
            model.Should().BeNull();
        }

        [AfterScenario]
        public async Task ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_apiTestContext.NewUserId)) return;
            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(_apiTestContext.NewUserId, _apiTestContext.GraphApiToken);
            _apiTestContext.NewUserId = null;
        }
    }
}
