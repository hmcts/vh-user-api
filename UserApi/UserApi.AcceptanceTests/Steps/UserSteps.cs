using System.Collections.Generic;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class UserSteps : BaseSteps
    {
        private readonly ScenarioContext _context;
        private readonly AcTestContext _acTestContext;
        private readonly UserEndpoints _endpoints = new ApiUriFactory().UserEndpoints;
        private readonly List<int> _numbers;
        private int _total;

        public UserSteps(ScenarioContext injectedContext, AcTestContext acTestContext)
        {
            _context = injectedContext;
            _acTestContext = acTestContext;
            _numbers = new List<int>();
        }

        [Given("I have entered (.*) into the calculator")]
        public void GivenIHaveEnteredSomethingIntoTheCalculator(int number)
        {
            _numbers.Add(number);
        }

        [When("I press add")]
        public void WhenIPressAdd()
        {
            foreach (var number in _numbers)
            {
                _total = _total + number;
            }
        }

        [Then("the result should be (.*) on the screen")]
        public void ThenTheResultShouldBe(int result)
        {
            result.Should().Be(_total);
        }
    }
}
