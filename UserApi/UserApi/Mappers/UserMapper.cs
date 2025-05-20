using System.Linq;
using Microsoft.Graph.Models;
using UserApi.Contract.Responses;

namespace UserApi.Mappers
{
    public static class UserMapper
    {
        public static UserResponse MapToUserResponse(User graphUser)
        {
            return new UserResponse
            {
                FirstName = graphUser.GivenName,
                LastName = graphUser.Surname,
                DisplayName = graphUser.DisplayName,
                Email = graphUser.UserPrincipalName,
                ContactEmail = graphUser.OtherMails?.FirstOrDefault(),
                TelephoneNumber = graphUser.MobilePhone,
                Organisation = graphUser.CompanyName
            };
        }

        public static UserProfile MapToUserProfile(User graphUser, string userRole, bool isUserAdmin)
        {
            return new UserProfile
            {
                UserId = graphUser.Id,
                UserName = graphUser.UserPrincipalName,
                Email = graphUser.Mail ?? graphUser.OtherMails?.FirstOrDefault(),
                DisplayName = graphUser.DisplayName,
                FirstName = graphUser.GivenName,
                LastName = graphUser.Surname,
                TelephoneNumber = graphUser.MobilePhone,
                UserRole = userRole,
                IsUserAdmin = isUserAdmin
            };
        }
    }
}