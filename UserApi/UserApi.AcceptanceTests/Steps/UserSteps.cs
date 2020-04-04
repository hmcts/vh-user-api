using System;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using AcceptanceTests.Common.Api.Helpers;
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
        private readonly TestContext _context;

        private string _newUsername;
        private readonly CommonSteps _commonSteps;

        public UserSteps(TestContext context, CommonSteps commonSteps)
        {
            _context = context;
            _commonSteps = commonSteps;
        }

        [Given(@"I have a new hearings reforms user account request with a valid email")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestWithAValidEmail()
        {
            _context.Request = _context.Post(CreateUser, new CreateUserRequestBuilder().Build());
        }

        [Given(@"I have a get user by AD user Id request for an existing user")]
        public void GivenIHaveAGetUserByAdUserIdRequestForAnExistingUser()
        {
            _context.Request = _context.Get(GetUserByAdUserId(_context.Config.TestSettings.ExistingUserId));
        }

        [Given(@"I have a get user by user principal name request for an existing user principal name")]
        public void GivenIHaveAGetUserByUserPrincipalNameRequestForAnExistingUserPrincipalName()
        {
            _context.Request = _context.Get(GetUserByAdUserName(_context.Config.TestSettings.ExistingUserPrincipal));
        }

        [Given(@"I have a new user")]
        public void GivenIHaveANewUser()
        {
            var model = CreateNewUser();
            _newUsername = model.Username;
            AddUserToExternalGroup(model.UserId);
            PollForUserInAad().Should().BeTrue("User has been created in AAD");
            PollForUserGroupAdded(model.UserId).Should().BeTrue("User added to group");
        }

        private NewUserResponse CreateNewUser()
        {
            _context.Request = _context.Post(CreateUser, new CreateUserRequestBuilder().Build());
            _commonSteps.WhenISendTheRequestToTheEndpoint();
            _commonSteps.ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode.Created, true);
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(_context.Response.Content);
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
            _context.Request = _context.Patch(AddUserToGroup, request);
            _commonSteps.WhenISendTheRequestToTheEndpoint();
            _commonSteps.ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode.Accepted, true);
        }

        private bool PollForUserInAad()
        {
            _context.Request = _context.Get(GetUserByAdUserName(_newUsername));
            for (var i = 0; i < Timeout; i++)
            {
                _commonSteps.WhenISendTheRequestToTheEndpoint();
                if (_context.Response.IsSuccessful)
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            return false;
        }

        private bool PollForUserGroupAdded(string userId)
        {
            _context.Request = _context.Get(GetGroupsForUser(userId));
            for (var i = 0; i < Timeout; i++)
            {
                _commonSteps.WhenISendTheRequestToTheEndpoint();
                var groups = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(_context.Response.Content);
                if (groups.Any(x => x.DisplayName.Equals("External")))
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            return false;
        }

        [Given(@"I have a delete user request for the new user")]
        public void GivenIHaveADeleteUserRequestForTheNewUser()
        {
            _context.Request = _context.Delete(DeleteUser(_newUsername));
        }

        [Given(@"I have a get user profile by email request for an existing email")]
        public void GivenIHaveAGetUserProfileByEmailRequestForAnExistingEmail()
        {
            _context.Request = _context.Get(GetUserByEmail(_context.Config.TestSettings.ExistingEmail));
        }

        [Given(@"I have a valid AD group id and request for a list of judges")]
        public void GivenIHaveAValidAdGroupIdAndRequestForAListOfJudges()
        {
            _context.Request = _context.Get(GetJudges());
        }

        [Given(@"I have a new hearings reforms user account request with an existing name")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestWithAnExistingFullName()
        {
            var request = new CreateUserRequestBuilder()
                .WithFirstname(_context.Config.TestSettings.ExistingUserFirstname)
                .WithLastname(_context.Config.TestSettings.ExistingUserLastname)
                .Build();
            _context.Request = _context.Post(CreateUser, request);
        }

        [Then(@"the user should be added")]
        public void ThenTheUserShouldBeAdded()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(_context.Response.Content);
            model.Should().NotBeNull();
            model.OneTimePassword.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.Username.Should().NotBeNullOrEmpty();
            _context.Test.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public void ThenTheUserDetailsShouldBeRetrieved()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(_context.Response.Content);
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

        private bool PollForUserDeleted()
        {
            _context.Request = _context.Get(GetUserByAdUserName(_newUsername));
            for (var i = 0; i < Timeout; i++)
            {
                _commonSteps.WhenISendTheRequestToTheEndpoint();
                if (_context.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            return false;
        }

        [Then(@"a list of ad judges should be retrieved")]
        public void ThenAListOfAdJudgesShouldBeRetrieved()
        {
            var actualJudges = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<UserResponse>>(_context.Response.Content);
            actualJudges.Should().NotBeNull();
            foreach (var user in actualJudges)
            {
                user.Email.Should().NotBeNullOrEmpty();
                user.DisplayName.Should().NotBeNullOrEmpty();
            }

            var expectedJudge = UserManager.GetJudgeUser(_context.UserAccounts);
            var actualJudge = actualJudges.First(u => u.Email.Equals(expectedJudge.Username));
            actualJudge.DisplayName.Should().Be(expectedJudge.DisplayName);
        }
        
        [Then(@"the list of ad judges should not contain performance test users")]
        public void TheListOfAdJudgesShouldNotContainPerformanceTestUsers()
        {
            var actualJudges = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<UserResponse>>(_context.Response.Content);
            actualJudges.Should().NotBeNull();
            actualJudges.Any(x => x.FirstName == "TP").Should().BeFalse();
        }
    }
}
