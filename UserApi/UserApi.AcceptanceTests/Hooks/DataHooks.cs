using System.Collections.Generic;
using System.Linq;
using System.Net;
using AcceptanceTests.Common.Api.Clients;
using AcceptanceTests.Common.Api.Requests;
using AcceptanceTests.Common.Model.UserGroup;
using FluentAssertions;
using RestSharp;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Helpers;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public static class DataHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.DataHooks)]
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
    }
}
