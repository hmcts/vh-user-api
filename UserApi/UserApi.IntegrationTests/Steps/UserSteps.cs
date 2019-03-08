using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Faker;
using FluentAssertions;
using TechTalk.SpecFlow;
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
        private readonly UserEndpoints _endpoints = new ApiUriFactory().UserEndpoints;

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
            var createUserRequest = new CreateUserRequest
            {
                RecoveryEmail = Internet.Email(),
                FirstName = Name.First(),
                LastName = Name.Last()
            };
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    break;
                }
                case Scenario.Existing:
                {
                    createUserRequest.RecoveryEmail = _apiTestContext.TestSettings.ExistingEmail;
                    createUserRequest.FirstName = _apiTestContext.TestSettings.ExistingFirstname;
                    createUserRequest.LastName = _apiTestContext.TestSettings.ExistingLastname;
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

        [Given(@"I have a get user by AD user Id request for a (.*) user")]
        [Given(@"I have a get user by AD user Id request for an (.*) user")]
        public void GivenIHaveAGetUserByADUserIdRequestForTheUser(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Existing:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId(_apiTestContext.TestSettings.ExistingUserId);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId(string.Empty);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
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
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetUserByAdUserId(string.Empty);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
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
                    _apiTestContext.Uri = _endpoints.GetUserByEmail(string.Empty);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Then(@"the user should be added")]
        public async Task ThenTheUserShouldBeAdded()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(json);
            model.Should().NotBeNull();
            _apiTestContext.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public async Task ThenTheUserDetailsShouldBeRetrieved()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(json);
            model.Should().NotBeNull();
            //model.CaseType.Should().NotBeNullOrEmpty();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.Email.Should().NotBeNullOrEmpty();
            model.FirstName.Should().NotBeNullOrEmpty();
            model.LastName.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.UserName.Should().NotBeNullOrEmpty();
            model.UserRole.Should().NotBeNullOrEmpty();
        }

        [Then(@"the response should be empty")]
        public async Task ThenTheResponseShouldBeEmpty()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(json);
            model.Should().BeNull();
        }

        [AfterScenario]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_apiTestContext.NewUserId)) return;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiTestContext.GraphApiToken);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                    $@"https://graph.microsoft.com/v1.0/users/{_apiTestContext.NewUserId}");
                var result = client.SendAsync(httpRequestMessage).Result;
                result.IsSuccessStatusCode.Should().BeTrue($"{_apiTestContext.NewUserId} should be deleted");
                _apiTestContext.NewUserId = null;
            }
        }
    }
}
