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
    }
}