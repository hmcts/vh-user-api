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
        private Settings _settings;
        protected const string Domain = "@hearings.test.server.net";

        [SetUp]
        public void Setup()
        {
            _accountService = new Mock<IUserAccountService>();
            _settings = new Settings
            {
                IsLive = true,
                ReformEmail = Domain.Replace("@", ""),
                AdGroup = new AdGroup
                {
                    Administrator = "Admin",
                    CaseType = "CT",
                    External = "Ext",
                    Judge = "JudgeGroup",
                    ProfessionalUser = "ProfUser",
                    JudgesTestGroup = "TA",
                    JudicialOfficeHolder = "JOH"
                }
            };
            _helper = new UserProfileHelper(_accountService.Object, _settings);
        }
        
        [Test]
        public async Task Should_return_case_admin_for_user_with_money_claims_group()
        {
            GivenFilterReturnsUserWithCaseTypeGroups("Civil Money Claims");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("CaseAdmin");
        }
        
        [Test]
        public async Task Should_return_case_admin_for_user_with_financial_remedy_group()
        {
            GivenFilterReturnsUserWithCaseTypeGroups("Financial Remedy");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("CaseAdmin");
        }

        [Test]
        public async Task Should_return_case_admin_for_user_with_generic_generic_group()
        {
            GivenFilterReturnsUserWithCaseTypeGroups("Generic");

            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("CaseAdmin");
        }

        [Test]
        public async Task Should_return_judge_for_user_with_internal_and_virtualroomjudge()
        {
            GivenFilterReturnsUserWithGroups("JudgeGroup");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("Judge");
        }
        
        [Test]
        public async Task Should_return_joh_for_joh_user()
        {
            GivenFilterReturnsUserWithGroups("JOH");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("JudicialOfficeHolder");
        }
        
        [Test]
        public async Task Should_return_vhadmin_for_user_with_internal_and_virtualroomadministrator()
        {
            GivenFilterReturnsUserWithGroups("Admin");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("VhOfficer");
        }
        
        [Test]
        public async Task Should_return_vhadmin_for_user_with_both_vho_groups_and_case_admin_group()
        {
            GivenFilterReturnsUserWithGroups("Admin", "Financial Remedy");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("VhOfficer");
        }
        
        [Test]
        public async Task Should_return_representative_for_user_with_external_and_virtualcourtroomprofessional_groups()
        {
            GivenFilterReturnsUserWithGroups("ProfUser");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("Representative");
        }

        [Test]
        public async Task Should_return_individual_for_user_with_external_group()
        {
            GivenFilterReturnsUserWithGroups("Ext");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("Individual");
        }
        
        [Test]
        public async Task Should_return_none_for_user_with_no_groups()
        {
            GivenFilterReturnsUserWithGroups();
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.UserRole.Should().Be("None");
        }
        
        [Test]
        public async Task Should_return_null_for_no_user_found()
        {
            _accountService.Setup(x => x.GetUserByFilterAsync(Filter)).ReturnsAsync((User) null);
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.Should().BeNull();
        }
        
        [Test]
        public async Task Should_return_case_types_for_case_admin()
        {
            GivenFilterReturnsUserWithCaseTypeGroups("Civil Money Claims", "Financial Remedy", "Generic");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.CaseType.Count.Should().Be(3);
            userProfile.CaseType.Should().Contain("Civil Money Claims");
            userProfile.CaseType.Should().Contain("Financial Remedy");
            userProfile.CaseType.Should().Contain("Generic");
        }
        
        [Test]
        public async Task Should_return_user_data()
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Mail = "bob@contoso.com",
                DisplayName = "Bob McGregor",
                GivenName = "Bob",
                Surname = "McGregor",
                UserPrincipalName = "bob.mcgregor@hearings.test.server.net"
            };

            GivenFilterReturnsUserWithGroups(user, null, "Ext");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.DisplayName.Should().Be(user.DisplayName);
            userProfile.FirstName.Should().Be(user.GivenName);
            userProfile.LastName.Should().Be(user.Surname);
            userProfile.Email.Should().Be(user.Mail);
            userProfile.UserId.Should().Be(user.Id);
            userProfile.UserName.Should().Be(user.UserPrincipalName);
        }
        
        [Test]
        public async Task Should_return_user_data_when_email_has_quotes()
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Mail = "bo'b@cont'oso.com",
                DisplayName = "Bob McGregor",
                GivenName = "Bob",
                Surname = "McGregor",
                UserPrincipalName = "bo'b.mcg'regor@hearings.test.server.net"
            };

            GivenFilterReturnsUserWithGroups(user, null, "Ext");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.DisplayName.Should().Be(user.DisplayName);
            userProfile.FirstName.Should().Be(user.GivenName);
            userProfile.LastName.Should().Be(user.Surname);
            userProfile.Email.Should().Be(user.Mail);
            userProfile.UserId.Should().Be(user.Id);
            userProfile.UserName.Should().Be(user.UserPrincipalName);
        }

        private void GivenFilterReturnsUserWithGroups(params string[] groupDisplayNames)
        {
            var user = new User { Id = Guid.NewGuid().ToString() };

            GivenFilterReturnsUserWithGroups(user, "test", groupDisplayNames);
        }

        private void GivenFilterReturnsUserWithGroups(User user, string description = null, params string[] groupDisplayNames)
        {
            _accountService.Setup(x => x.GetUserByFilterAsync(Filter)).ReturnsAsync(user);

            var groups = groupDisplayNames.Select(aadGroup => new Group { DisplayName = aadGroup, Description = description }).ToArray();

            _accountService.Setup(x => x.GetGroupsForUserAsync(user.Id)).ReturnsAsync(new List<Group>(groups));
        }

        private void GivenFilterReturnsUserWithCaseTypeGroups(params string[] groupDisplayNames)
        {
            var user = new User { Id = Guid.NewGuid().ToString() };

            GivenFilterReturnsUserWithGroups(user, "CT", groupDisplayNames);
        }
    }
}