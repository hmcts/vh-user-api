using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.AcceptanceTests.Helpers;
using UserApi.Contract.Responses;
using UserApi.Security;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public class BaseSteps
    {
        protected BaseSteps(){}

        [BeforeTestRun]
        public static void OneTimeSetup(TestContext context)
        {
            var azureAdConfiguration = TestConfig.Instance.AzureAd;
            context.TestSettings = TestConfig.Instance.TestSettings;

            context.BearerToken = new TokenProvider(azureAdConfiguration).GetClientAccessToken(
                context.TestSettings.TestClientId, context.TestSettings.TestClientSecret,
                azureAdConfiguration.VhUserApiResourceId);

            context.GraphApiToken = new TokenProvider(azureAdConfiguration).GetClientAccessToken(
                azureAdConfiguration.ClientId, azureAdConfiguration.ClientSecret,
                "https://graph.microsoft.com");

            var apiTestsOptions = TestConfig.Instance.GetFromSection<AcceptanceTestConfiguration>("AcceptanceTestSettings");
            context.BaseUrl = apiTestsOptions.UserApiBaseUrl;
        }

        [BeforeTestRun]
        public static void CheckHealth(TestContext context)
        {
            var endpoint = new ApiUriFactory().HealthCheckEndpoints;
            context.Request = context.Get(endpoint.CheckServiceHealth());
            context.Response = context.Client().Execute(context.Request);
            context.Response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [BeforeTestRun]
        public static void CheckUserExistsWithCorrectGroups(TestContext context)
        {
            var endpoint = new ApiUriFactory().AccountEndpoints;
            context.Request = context.Get(endpoint.GetGroupsForUser(context.TestSettings.ExistingUserId));
            context.Response = context.Client().Execute(context.Request);
            context.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(context.Response.Content);
            var actualGroups = model.Select(@group => new Group() {GroupId = @group.GroupId, DisplayName = @group.DisplayName}).ToList();
            context.TestSettings.ExistingGroups.Should().BeEquivalentTo(actualGroups, opts => opts.WithoutStrictOrdering());
        }
    }
}
