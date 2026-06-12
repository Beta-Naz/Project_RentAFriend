using Newtonsoft.Json;

namespace RentAFriendApp.Models
{
    public class BoolResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
