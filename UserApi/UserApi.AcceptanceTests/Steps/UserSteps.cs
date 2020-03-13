using System;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using AcceptanceTests.Common.Api.Requests;
using AcceptanceTests.Common.Configuration.Users;
using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.Services.Models;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;
using static Testing.Common.Helpers.UserApiUriFactory.UserEndpoints;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class UserSteps
    {
        private const int Timeout = 60;
        private readonly TestContext _c;
        private readonly CommonSteps _commonSteps;

        public UserSteps(TestContext context, CommonSteps commonSteps)
        {
            _c = context;
            _commonSteps = commonSteps;
        }

        [Given(@"I have a new hearings reforms user account request with a valid email")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestWithAValidEmail()
        {
            _c.Request = RequestBuilder.Post(CreateUser, new CreateUserRequestBuilder().Build());
        }

        [Given(@"I have a get user by AD user Id request for an existing user")]
        public void GivenIHaveAGetUserByAdUserIdRequestForAnExistingUser()
        {
            var endpoint = GetUserByAdUserId(_c.UserApiConfig.TestConfig.ExistingUserId);
            _c.Request = RequestBuilder.Get(endpoint);
        }

        [Given(@"I have a get user by user principal name request for an existing user principal name")]
        public void GivenIHaveAGetUserByUserPrincipalNameRequestForAnExistingUserPrincipalName()
        {
            var endpoint = GetUserByAdUserName(_c.UserApiConfig.TestConfig.ExistingUserPrincipal);
            _c.Request = RequestBuilder.Get(endpoint);
        }

        [Given(@"I have a new user")]
        public void GivenIHaveANewUser()
        {
            var model = CreateNewUser();
            _c.Test.NewUsername = model.Username;
            AddUserToExternalGroup(model.UserId);
            PollForUserInAad().Should().BeTrue("User has been created in AAD");
            PollForUserGroupAdded(model.UserId).Should().BeTrue("User added to group");
        }

        private NewUserResponse CreateNewUser()
        {
            _c.Request = RequestBuilder.Post(CreateUser, new CreateUserRequestBuilder().Build());
            _commonSteps.WhenISendTheRequestToTheEndpoint();
            _commonSteps.ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode.Created, true);
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(_c.Response.Content);
            model.Username.Should().NotBeNullOrEmpty();
            return model;
        }

        private void AddUserToExternalGroup(string userId)
        {
            var request = new AddUserToGroupRequest()
            {
                UserId = userId,
                GroupName = "External"
            };
            _c.Request = RequestBuilder.Patch(AddUserToGroup, request);
            _commonSteps.WhenISendTheRequestToTheEndpoint();
            _commonSteps.ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode.Accepted, true);
        }

        [Given(@"I have a delete user request for the new user")]
        public void GivenIHaveADeleteUserRequestForTheNewUser()
        {
            _c.Request = RequestBuilder.Delete(DeleteUser(_c.Test.NewUsername));
        }

        [Given(@"I have a get user profile by email request for an existing email")]
        public void GivenIHaveAGetUserProfileByEmailRequestForAnExistingEmail()
        {
            _c.Request = RequestBuilder.Get(GetUserByEmail(_c.UserApiConfig.TestConfig.ExistingEmail));
        }

        [Given(@"I have a valid AD group id and request for a list of judges")]
        public void GivenIHaveAValidAdGroupIdAndRequestForAListOfJudges()
        {
            _c.Request = RequestBuilder.Get(GetJudges());
        }

        [Given(@"I have a new hearings reforms user account request with an existing name")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestWithAnExistingFullName()
        {
            var request = new CreateUserRequestBuilder()
                .WithFirstname(_c.UserApiConfig.TestConfig.ExistingUserFirstname)
                .WithLastname(_c.UserApiConfig.TestConfig.ExistingUserLastname)
                .Build();
            _c.Request = RequestBuilder.Post(CreateUser, request);
        }

        [Then(@"the user should be added")]
        public void ThenTheUserShouldBeAdded()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(_c.Response.Content);
            model.Should().NotBeNull();
            model.OneTimePassword.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.Username.Should().NotBeNullOrEmpty();
            _c.Test.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public void ThenTheUserDetailsShouldBeRetrieved()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(_c.Response.Content);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.Email.Should().NotBeNullOrEmpty();
            model.FirstName.Should().NotBeNullOrEmpty();
            model.LastName.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.UserName.Should().NotBeNullOrEmpty();
        }

        [Then(@"the new user should be deleted")]
        public void ThenTheNewUserShouldBeDeleted()
        {
            PollForUserDeleted().Should().BeTrue("User has been successfully deleted");
        }

        [Then(@"a list of ad judges should be retrieved")]
        public void ThenAListOfAdJudgesShouldBeRetrieved()
        {
            var actualJudges = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<UserResponse>>(_c.Response.Content);
            actualJudges.Should().NotBeNull();
            foreach (var user in actualJudges)
            {
                user.Email.Should().NotBeNullOrEmpty();
                user.DisplayName.Should().NotBeNullOrEmpty();
            }

            var expectedJudge = UserManager.GetJudgeUser(_c.UserAccounts);
            var actualJudge = actualJudges.First(u => u.Email.Equals(expectedJudge.Username));
            actualJudge.DisplayName.Should().Be(expectedJudge.DisplayName);
        }

        private bool PollForUserDeleted()
        {
            _c.Request = RequestBuilder.Get(GetUserByAdUserName(_c.Test.NewUsername));
            for (var i = 0; i < Timeout; i++)
            {
                _commonSteps.WhenISendTheRequestToTheEndpoint();
                if (_c.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            return false;
        }

        private bool PollForUserInAad()
        {
            _c.Request = RequestBuilder.Get(GetUserByAdUserName(_c.Test.NewUsername));
            for (var i = 0; i < Timeout; i++)
            {
                _commonSteps.WhenISendTheRequestToTheEndpoint();
                if (_c.Response.IsSuccessful)
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            return false;
        }

        private bool PollForUserGroupAdded(string userId)
        {
            _c.Request = RequestBuilder.Get(GetGroupsForUser(userId));
            for (var i = 0; i < Timeout; i++)
            {
                _commonSteps.WhenISendTheRequestToTheEndpoint();
                var groups = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(_c.Response.Content);
                if (groups.Any(x => x.DisplayName.Equals("External")))
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            return false;
        }
    }
}
