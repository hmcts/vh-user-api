using System.Collections.Generic;
using Newtonsoft.Json;

namespace UserApi.Services.Models
{
    public class AzureAdGraphQueryResponse<T>
    {
        [JsonProperty("@odata.metadata")] public string Context { get; set; }

        [JsonProperty("value")] public IList<T> Value { get; set; }
    }

    public class AzureAdGraphUserResponse
    {
        public string ObjectId { get; set; }
        public string UserPrincipalName { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public List<string> OtherMails { get; set; }
        [JsonProperty("mobile")]
        public string MobilePhone { get; set; }
        [JsonProperty("mail")]
        public string ContactEmail { get; set; }
    }
    
    public class UserAssignedRole
    {
        public string Id { get; set; }
        public string PrincipalId { get; set; }
        public string PrincipalOrganizationId { get; set; }
        public string ResourceScope { get; set; }
        public string RoleDefinitionId { get; set; }
    }
  
}