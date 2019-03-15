using System.Collections.Generic;

namespace UserApi.Services.Models
{
    public class UserProfile
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserRole { get; set; }
        public List<string> CaseType { get; set; }
    }
}