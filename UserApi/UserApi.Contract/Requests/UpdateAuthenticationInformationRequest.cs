using Newtonsoft.Json;
using System.Collections.Generic;

namespace UserApi.Contract.Requests
{
    public class UpdateAuthenticationInformationRequest
    {
        [JsonProperty("otherMails")]
        public List<string> OtherMails { get; set; }
    }
}