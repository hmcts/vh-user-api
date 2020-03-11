using AcceptanceTests.Common.Model.UserRole;
using UserApi.Contract.Responses;

namespace UserApi.IntegrationTests.Configuration
{
    public class Test
    {
        public string NewUserId { get; set; }
        public NewUserResponse NewUser { get; set; }
        public UserRole UserRole { get; set; }
    }
}
