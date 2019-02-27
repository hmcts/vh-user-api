using System.Collections.Generic;
using Newtonsoft.Json;

namespace UserApi.Contract.Requests
{
    public class UpdateAuthenticationInformationRequest
    {
        [JsonProperty("otherMails")] public List<string> OtherMails { get; set; }
    }
}