using Newtonsoft.Json;

namespace UserApi.Services.Models
{
    public class CustomDirectoryObject
    {
        [JsonProperty(PropertyName = "@odata.id")]
        public string ObjectDataId { get; set; }
    }
}