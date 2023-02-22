using System.Collections.Generic;

namespace UserApi.Contract.Responses
{
    public class UserProfile
    {
        /// <summary>
        /// The user's object ID
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// The user's username
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// The user's contact email
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// The user's display name
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// The user's first name
        /// </summary>
        public string FirstName { get; set; }
        
        /// <summary>
        /// The user's last name
        /// </summary>
        public string LastName { get; set; }
        
        /// <summary>
        /// The user's role (one of UserRole)
        /// </summary>
        public string UserRole { get; set; }
        
        /// <summary>
        /// The user's assigned case types
        /// </summary>
        public List<string> CaseType { get; set; }
        
        /// <summary>
        /// The user has the Administrator role assignment 
        /// </summary>
        public bool IsUserAdmin { get; set; }
        
        /// <summary>
        /// The user's contact telephone number
        /// </summary>
        public string TelephoneNumber { get; set; }
    }
}