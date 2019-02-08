using System.Collections.Generic;
using Newtonsoft.Json;

namespace UserApi.Contracts.Requests
{
    public class UpdateAuthenticationInformationRequest
    {
        [JsonProperty("otherMails")]
        public List<string> OtherMails { get; set; }
    }
}