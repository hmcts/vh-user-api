using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Faker;
using FluentAssertions;
using NUnit.Framework;
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
        private readonly ScenarioContext _scenarioContext;
        private readonly ApiTestContext _apiTestContext;
        private readonly UserEndpoints _endpoints = new ApiUriFactory().UserEndpoints;
        private const string ExistingUser = "60c7fae1-8733-4d82-b912-eece8d55d54c";
        private const string ExistingUserPrinciple = "VirtualRoomAdministrator@hearings.reform.hmcts.net";
        private const string ExistingEmail = "VirtualRoomAdministrator@kinley.com";

        public UserSteps(ScenarioContext scenarioContext, ApiTestContext apiTestContext)
        {
            _scenarioContext = scenarioContext;
            _apiTestContext = apiTestContext;
        }

        [Given(@"I have a new hearings reforms user account request for a (.*) user")]
        [Given(@"I have a new hearings reforms user account request for an (.*) user")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestForTheUser(Scenario scenario)
        {
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
                    break;
                }
                case Scenario.Nonexistent:
                    break;
                case Scenario.Invalid:
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }


           
            _apiTestContext.StringContent = new StringContent(
                ApiRequestHelper.SerialiseRequestToSnakeCaseJson(createUserRequest),
                Encoding.UTF8, "application/json");
        }

        [Given(@"I have a get user by AD user Id request for the user '(.*)'")]
        public void GivenIHaveAGetUserByADUserIdRequestForTheUser(string username)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            _apiTestContext.Uri = _endpoints.GetUserByAdUserId(username);
        }

        [Given(@"I have a get user by user principle name request for the user principle name '(.*)'")]
        public void GivenIHaveAGetUserByUserPrincipleNameRequestForTheUserPrincipleName(string userPrincipleName)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            _apiTestContext.Uri = _endpoints.GetUserByAdUserName(userPrincipleName);
        }

        [Given(@"I have a get user profile by email request for the email '(.*)'")]
        public void GivenIHaveAGetUserProfileByEmailRequestForTheEmail(string email)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            _apiTestContext.Uri = _endpoints.GetUserByEmail(email);
        }

        [Then(@"the user should be added")]
        public async Task ThenTheUserShouldBeAdded()
        {

            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(json);
            model.Should().NotBeNull();
            _apiTestContext.NewUserId = model.UserId;
        }

        [Then(@"the user should not be added")]
        public void ThenTheUserShouldNotBeAdded()
        {
            ScenarioContext.Current.Pending();
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
