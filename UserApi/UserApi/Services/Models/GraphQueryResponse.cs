using System.Collections.Generic;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace UserApi.Services.Models
{
    public class GraphQueryResponse<T>
    {
        [JsonProperty("@odata.context")] public string Context { get; set; }

        [JsonProperty("value")] public IList<T> Value { get; set; }
    }
    
    public class GraphUserResponse
    {
        public string Id { get; set; }
        public string UserPrincipalName { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public List<string> OtherMails { get; set; }
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