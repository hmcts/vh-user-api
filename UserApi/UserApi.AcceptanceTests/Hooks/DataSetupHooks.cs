using Testing.Common;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public static class DataSetupHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.DataSetupHooks)]
        public static void CheckUserExistsWithCorrectGroups(TestContext context)
        {
            context.Request = TestContext.Get(GetGroupsForUser(context.Config.TestSettings.ExistingUserId));
            context.Response = context.Client().Execute(context.Request);
            context.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            var model = ApiRequestHelper.Deserialise<List<GroupsResponse>>(context.Response.Content);
            var actualGroups = model.Select(@group => new Group() { GroupId = @group.GroupId, DisplayName = @group.DisplayName }).ToList();
            foreach (var expectedGroup in context.Config.TestSettings.ExistingGroups)
            {
                actualGroups.Exists(x => x.DisplayName.Equals(expectedGroup.DisplayName)).Should().BeTrue();
            }
        }
    }
}
