using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Clients;
using AcceptanceTests.Common.Api.Requests;
using AcceptanceTests.Common.Model.UserGroup;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public static class DataHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.CheckGroupsHooks)]
        public static void CheckUserExistsWithCorrectGroups(TestContext context)
        {
            var endpoint = GetGroupsForUser(context.UserApiConfig.TestConfig.ExistingUserId);
            context.Request = RequestBuilder.Get(endpoint);
            var client = ApiClient.SetClient(context.UserApiConfig.VhServices.UserApiUrl, context.BearerToken);
            context.Response = RequestExecutor.SendToApi(context.Request, client);
            context.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(context.Response.Content);
            var actualGroups = model.Select(@group => new Group() { GroupId = @group.GroupId, DisplayName = @group.DisplayName }).ToList();
            context.UserApiConfig.TestConfig.ExistingGroups.Should().BeEquivalentTo(actualGroups, opts => opts.WithoutStrictOrdering());
        }

        [BeforeScenario(Order = (int)HooksSequence.RemoveGroup)]
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
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(context.UserApiConfig.TestConfig.ExistingUserId,
                context.UserApiConfig.TestConfig.NewGroups.First().DisplayName, context.GraphApiToken);
            if (userIsInTheGroup)
            {
                await ActiveDirectoryUser.RemoveTheUserFromTheGroupAsync(context.UserApiConfig.TestConfig.ExistingUserId,
                    context.UserApiConfig.TestConfig.NewGroups.First().GroupId, context.GraphApiToken);
            }
            context.Test.NewGroupId = null;
        }

        [AfterScenario]
        public static async Task NewUserClearUp(TestContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Test.NewUserId)) return;
            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(context.Test.NewUserId, context.GraphApiToken);
            context.Test.NewUserId = null;
        }
    }
}
