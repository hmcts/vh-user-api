using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using TechTalk.SpecFlow;
using TestContext = UserApi.IntegrationTests.Contexts.TestContext;

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class CommonSteps : BaseSteps
    {
        private readonly TestContext _testContext;

        public CommonSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [When(@"I send the request to the endpoint")]
        [When(@"I send the same request twice")]
        public async Task WhenISendTheRequestToTheEndpoint()
        {
            _testContext.ResponseMessage = new HttpResponseMessage();
            _testContext.ResponseMessage = _testContext.HttpMethod.Method switch
            {
                "GET" => await SendGetRequestAsync(_testContext),
                "POST" => await SendPostRequestAsync(_testContext),
                "PATCH" => await SendPatchRequestAsync(_testContext),
                "PUT" => await SendPutRequestAsync(_testContext),
                "DELETE" => await SendDeleteRequestAsync(_testContext),
                _ => throw new ArgumentOutOfRangeException(_testContext.HttpMethod.ToString(),
                    _testContext.HttpMethod.ToString(), null)
            };
        }

        [Then(@"the response should have the status (.*) and success status (.*)")]
        public void ThenTheResponseShouldHaveStatus(HttpStatusCode statusCode, bool isSuccess)
        {
            _testContext.ResponseMessage.StatusCode.Should().Be(statusCode);
            _testContext.ResponseMessage.IsSuccessStatusCode.Should().Be(isSuccess);
            NUnit.Framework.TestContext.WriteLine($"Status Code: {_testContext.ResponseMessage.StatusCode}");
        }

        [Then(@"the response message should read '(.*)'")]
        [Then(@"the error response message should contain '(.*)'")]
        [Then(@"the error response message should also contain '(.*)'")]
        public void ThenTheResponseShouldContain(string errorMessage)
        {
            _testContext.ResponseMessage.Content.ReadAsStringAsync().Result.Should().Contain(errorMessage);
        }
    }
}
