using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class UpdateProfileResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
