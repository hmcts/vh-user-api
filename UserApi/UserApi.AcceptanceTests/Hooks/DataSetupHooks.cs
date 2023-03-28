using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common;
using Testing.Common.ActiveDirectory;
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
            context.Request = context.Get(GetGroupsForUser(context.Config.TestSettings.ExistingUserId));
            context.Response = context.Client().Execute(context.Request);
            context.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            var model = RequestHelper.Deserialise<List<GroupsResponse>>(context.Response.Content);
            var actualGroups = model.Select(@group => new Group() { GroupId = @group.GroupId, DisplayName = @group.DisplayName }).ToList();
            foreach (var expectedGroup in context.Config.TestSettings.ExistingGroups)
            {
                actualGroups.Any(x => x.DisplayName.Equals(expectedGroup.DisplayName)).Should().BeTrue();
            }
        }

        [BeforeScenario(Order = (int)HooksSequence.RemoveGroupsHooks)]
        public static async Task RemoveNewGroupIfExistsAsync(TestContext context)
        {
            await RemoveGroupFromUserIfExists(context);
        }

        [AfterScenario]
        public static async Task RemoveNewGroupAgainIfExists(TestContext context)
        {
            await RemoveGroupFromUserIfExists(context);
            context.Test.NewGroupId = null;
        }

        private static async Task RemoveGroupFromUserIfExists(TestContext context)
        {
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(context.Config.TestSettings.ExistingUserId,
                context.Config.TestSettings.NewGroups.First().DisplayName, context.Tokens.GraphApiBearerToken);
         
            if (userIsInTheGroup)
            {
                await ActiveDirectoryUser.RemoveTheUserFromTheGroupAsync(context.Config.TestSettings.ExistingUserId,
                    context.Config.TestSettings.NewGroups.First().GroupId, context.Tokens.GraphApiBearerToken);
            }
            context.Test.NewGroupId = null;
        }
    }
}
