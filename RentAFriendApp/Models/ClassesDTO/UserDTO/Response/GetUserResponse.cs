using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.UserDTO.Response
{
    public class GetUserResponse
    {
        [JsonProperty("data")]
        public UserLoginDTO? Data { get; set; }

        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }
}
