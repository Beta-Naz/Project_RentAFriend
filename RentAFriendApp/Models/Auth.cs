using Newtonsoft.Json;

namespace RentAFriendApp.Models
{
    public class Auth
    {
        [JsonProperty("fullname")]
        public string FullName { get; set; } = string.Empty;
        [JsonProperty("token")]
        public string Token {  get; set; } = string.Empty;
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
