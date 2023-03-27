using System.Collections.Generic;
using System.Linq;
using UserApi.Contract.Responses;
using GraphUser = Microsoft.Graph.User;

namespace UserApi.Mappers
{
    public static class GraphUserMapper
    {
        public static UserResponse MapToUserResponse(GraphUser graphUser)
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

        public static UserProfile MapToUserProfile(GraphUser graphUser)
        {
            return new UserProfile
            {
                UserId = graphUser.Id,
                UserName = graphUser.UserPrincipalName,
                Email = graphUser.Mail,
                DisplayName = graphUser.DisplayName,
                FirstName = graphUser.GivenName,
                LastName = graphUser.Surname,
                TelephoneNumber = graphUser.MobilePhone,
            };
        }
    }
}