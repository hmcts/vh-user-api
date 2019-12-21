using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.AcceptanceTests.Helpers;
using UserApi.Common;
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
            context.TestSettings = TestConfig.Instance.TestSettings;
            var tokenProvider = new TokenProvider(TestConfig.Instance.AzureAd);

            context.UserApiToken = tokenProvider.GetClientAccessToken
            (
                TestConfig.Instance.AzureAd.TenantId,
                TestConfig.Instance.AzureAd.ClientId,
                TestConfig.Instance.AzureAd.ClientSecret,
                new []{ $"{TestConfig.Instance.AzureAd.Scope}/.default"}
            );

            context.GraphApiToken = tokenProvider.GetClientAccessToken
            (
                TestConfig.Instance.AzureAd.AzureAdGraphApiConfig.TenantId,
                TestConfig.Instance.AzureAd.AzureAdGraphApiConfig.ClientId,
                TestConfig.Instance.AzureAd.AzureAdGraphApiConfig.ClientSecret,
                new []{ $"{TestConfig.Instance.AzureAd.GraphApiBaseUri}.default"}
            );

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
