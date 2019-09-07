using System.Threading.Tasks;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Responses;
using UserApi.Services.Models;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class UserSteps : BaseSteps
    {
        private readonly TestContext _context;
        private readonly UserEndpoints _endpoints = new ApiUriFactory().UserEndpoints;

        public UserSteps(TestContext context)
        {
            _context = context;
        }

        [Given(@"I have a new hearings reforms user account request with a valid email")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestWithAValidEmail()
        {
            _context.Request = _context.Post(_endpoints.CreateUser, new CreateUserRequestBuilder().Build());
        }

        [Given(@"I have a get user by AD user Id request for an existing user")]
        public void GivenIHaveAGetUserByAdUserIdRequestForAnExistingUser()
        {
            _context.Request = _context.Get(_endpoints.GetUserByAdUserId(_context.TestSettings.ExistingUserId));
        }

        [Given(@"I have a get user by user principal name request for an existing user principal name")]
        public void GivenIHaveAGetUserByUserPrincipalNameRequestForAnExistingUserPrincipalName()
        {
            _context.Request = _context.Get(_endpoints.GetUserByAdUserName(_context.TestSettings.ExistingUserPrincipal));
        }

        [Given(@"I have a get user profile by email request for an existing email")]
        public void GivenIHaveAGetUserProfileByEmailRequestForAnExistingEmail()
        {
            _context.Request = _context.Get(_endpoints.GetUserByEmail(_context.TestSettings.ExistingEmail));
        }

        [Given(@"I have a valid AD group id and request for a list of judges")]
        public void GivenIHaveAValidAdGroupIdAndRequestForAListOfJudges()
        {
            _context.Request = _context.Get(_endpoints.GetJudges());
        }

        [Given(@"I have a new hearings reforms user account request with an existing name")]
        public void GivenIHaveANewHearingsReformsUserAccountRequestWithAnExistingFullName()
        {
            var request = new CreateUserRequestBuilder()
                .WithFirstname(_context.TestSettings.ExistingUserFirstname)
                .WithLastname(_context.TestSettings.ExistingUserLastname)
                .Build();
            _context.Request = _context.Post(_endpoints.CreateUser, request);
        }

        [Then(@"the user should be added")]
        public void ThenTheUserShouldBeAdded()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<NewUserResponse>(_context.Json);
            model.Should().NotBeNull();
            model.OneTimePassword.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.Username.Should().NotBeNullOrEmpty();
            _context.NewUserId = model.UserId;
        }

        [Then(@"the user details should be retrieved")]
        public void ThenTheUserDetailsShouldBeRetrieved()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserProfile>(_context.Json);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.Email.Should().NotBeNullOrEmpty();
            model.FirstName.Should().NotBeNullOrEmpty();
            model.LastName.Should().NotBeNullOrEmpty();
            model.UserId.Should().NotBeNullOrEmpty();
            model.UserName.Should().NotBeNullOrEmpty();
        }

        [Then(@"a list of ad judges should be retrieved")]
        public void ThenAListOfAdJudgesShouldBeRetrieved()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<UserResponse>>(_context.Json);
            model.Should().NotBeNull();
            foreach (var user in model)
            {
                user.Email.Should().NotBeNullOrEmpty();
                user.DisplayName.Should().NotBeNullOrEmpty();
            }
            var expectedUser = model.FirstOrDefault(u => u.Email.Equals(_context.TestSettings.Judge));
            expectedUser.DisplayName.Should().Be("Automation01 Judge01");
        }

        [AfterScenario]
        public async Task NewUserClearUp()
        {
            if (string.IsNullOrWhiteSpace(_context.NewUserId)) return;
            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(_context.NewUserId, _context.GraphApiToken);
            _context.NewUserId = null;
        }
    }
}
