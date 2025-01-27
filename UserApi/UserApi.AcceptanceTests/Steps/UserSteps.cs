using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.UserEndpoints;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class UserSteps
    {
        private readonly TestContext _context;
        private string _newUsername;

        public UserSteps(TestContext context, CommonSteps commonSteps)
        {
            _context = context;
        }
        
        [Given(@"I have an existing user")]
        public void GivenIHaveAExistingUser()
        {
            _context.Request = TestContext.Get(GetUserByAdUserId(_context.Config.TestSettings.ExistingUserId));
            _context.Response = _context.Client().Execute(_context.Request);
            var model = ApiRequestHelper.Deserialise<UserProfile>(_context.Response.Content);
            _context.Test.NewUserId = model.UserId;
            _newUsername = model.UserName;
        }

        [Given(@"I have an update user request for the new user")]
        public void GivenIHaveAnUpdateUserRequestForTheNewUser()
        {
            _context.Request = TestContext.Patch(ResetUserPassword(), _newUsername);
        }
        
        [Given(@"I have an update user details request for the new user")]
        public void GivenIHaveAnUpdateUserDetailsRequestForTheNewUser()
        {
            var body = new UpdateUserAccountRequest
            {
                FirstName = "AcUpdatedFirstName",
                LastName = "ACUpdatedLastName"
            };
            var userId = Guid.Parse(_context.Test.NewUserId);
            _context.Request = TestContext.Patch(UpdateUserAccount(userId), body);
        }

        [Given(@"I have a valid AD group id and request for a list of judges")]
        public void GivenIHaveAValidAdGroupIdAndRequestForAListOfJudges()
        {
            _context.Request = TestContext.Get(GetJudgesByUsername());
        }
                
        [Then(@"a list of ad judges should be retrieved")]
        public void ThenAListOfAdJudgesShouldBeRetrieved()
        {
            var judges = ApiRequestHelper.Deserialise<List<UserResponse>>(_context.Response.Content);
            judges.Should().NotBeEmpty();
            foreach (var judge in judges)
            {
                judge.Email.Should().NotBeNullOrEmpty();
                judge.DisplayName.Should().NotBeNullOrEmpty();
            }
        }
        
        [Then(@"the list of ad judges should not contain performance test users")]
        public void TheListOfAdJudgesShouldNotContainPerformanceTestUsers()
        {
            var judges = ApiRequestHelper.Deserialise<List<UserResponse>>(_context.Response.Content);
            judges.Should().NotBeNull();
            judges.Exists(x => x.FirstName == "TP").Should().BeFalse();
        }
    }
}