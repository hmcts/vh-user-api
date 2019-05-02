using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using UserApi.Helper;
using UserApi.Services;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace UserApi.UnitTests.Helpers
{
    public class UserProfileHelperTests
    {
        private Mock<IUserAccountService> _accountService;
        private UserProfileHelper _helper;

        private const string Filter = "some filter";

        [SetUp]
        public void Setup()
        {
            _accountService = new Mock<IUserAccountService>();
            _helper = new UserProfileHelper(_accountService.Object);
        }
        
        [Test]
        public async Task should_return_case_admin_for_user_with_money_claims_group()
        {
            GivenFilterReturnsUserWithGroups("MoneyClaims");
            
            var userProfile = await _helper.GetUserProfile(Filter);

            userProfile.UserRole.Should().Be("CaseAdmin");
        }
        
        [Test]
        public async Task should_return_case_admin_for_user_with_financial_remedy_group()
        {
            GivenFilterReturnsUserWithGroups("FinancialRemedy");
            
            var userProfile = await _helper.GetUserProfile(Filter);

            userProfile.UserRole.Should().Be("CaseAdmin");
        }
        
        [Test]
        public async Task should_return_judge_for_user_with_internal_and_virtualroomjudge()
        {
            GivenFilterReturnsUserWithGroups("Internal", "VirtualRoomJudge");
            
            var userProfile = await _helper.GetUserProfile(Filter);

            userProfile.UserRole.Should().Be("Judge");
        }
        
        [Test]
        public async Task should_return_vhadmin_for_user_with_internal_and_virtualroomadministrator()
        {
            GivenFilterReturnsUserWithGroups("Internal", "VirtualRoomAdministrator");
            
            var userProfile = await _helper.GetUserProfile(Filter);

            userProfile.UserRole.Should().Be("VhOfficer");
        }
        
        [Test]
        public async Task should_return_representative_for_user_with_external_and_virtualcourtroomprofessional_groups()
        {
            GivenFilterReturnsUserWithGroups("External", "VirtualRoomProfessionalUser");
            
            var userProfile = await _helper.GetUserProfile(Filter);

            userProfile.UserRole.Should().Be("Representative");
        }

        [Test]
        public async Task should_return_individual_for_user_with_external_group()
        {
            GivenFilterReturnsUserWithGroups("External");
            
            var userProfile = await _helper.GetUserProfile(Filter);

            userProfile.UserRole.Should().Be("Individual");
        }

        private void GivenFilterReturnsUserWithGroups(params string[] groupDisplayNames)
        {
            var user = new User {Id = Guid.NewGuid().ToString()};
            
            _accountService.Setup(x => x.GetUserByFilter(Filter))
                .ReturnsAsync(user);

            var groups = groupDisplayNames.Select(displayName => new Group { DisplayName = displayName }).ToArray();
            _accountService.Setup(x => x.GetGroupsForUser(user.Id))
                .ReturnsAsync(new List<Group>(groups));
        }        
    }
}