using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class UpdateUserAccountTests : UserAccountServiceTests
    {
        [Test]
        public async Task Should_update_user_account()
        {
            // Arrange
            GraphUserResponse.Id = Guid.NewGuid().ToString();
            var graphQueryResponse = new GraphQueryResponse<GraphUserResponse>
            {
                Context = "context",
                Value = new List<GraphUserResponse> { GraphUserResponse }
            };
            var userId = GraphUserResponse.Id;
            const string firstName = "FirstName";
            const string lastName = "LastName";
            const string username = "username@email.com";
            const string contactEmail = "email@email.com";
            var updatedUser = new User
            {
                Id = userId,
                GivenName = firstName,
                Surname = lastName,
                UserPrincipalName = username,
                Mail = contactEmail,
                OtherMails = new List<string> { contactEmail }
            };
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse, HttpStatusCode.OK));
            IdentityServiceApiClient.Setup(x => x.UpdateUserAccount(userId,
                    firstName, lastName, It.IsAny<string>(),
                    contactEmail))
                .ReturnsAsync(updatedUser);
            
            // Act
            var result = await Service.UpdateUserAccountAsync(Guid.Parse(userId), firstName, lastName, contactEmail);

            // Assert
            result.Id.Should().Be(updatedUser.Id);
            result.GivenName.Should().Be(updatedUser.GivenName);
            result.Surname.Should().Be(updatedUser.Surname);
            result.UserPrincipalName.Should().Be(updatedUser.UserPrincipalName);
            result.Mail.Should().Be(updatedUser.Mail);
            result.OtherMails.Should().BeEquivalentTo(updatedUser.OtherMails);
        }

        [Test]
        public void Should_throw_exception_when_user_does_not_exist()
        {
            // Arrange
            var graphQueryResponse = new GraphQueryResponse<GraphUserResponse>
            {
                Context = "context",
                Value = new List<GraphUserResponse>()
            };
            var userId = Guid.NewGuid();
            const string firstName = "FirstName";
            const string lastName = "LastName";
            const string contactEmail = "email@email.com";
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse, HttpStatusCode.OK));
            
            // Act and Assert
            Assert.ThrowsAsync<UserDoesNotExistException>(() => Service.UpdateUserAccountAsync(userId, 
                firstName, lastName, contactEmail: contactEmail));
        }
    }
}
