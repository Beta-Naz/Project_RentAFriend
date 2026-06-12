using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class CreateProfileResponse
    {
        [JsonProperty("Message")]
        public string Message { get; set; } = string.Empty;
        [JsonProperty("friendProfile")]
        public FPInfoDTO FriendProfile { get; set; } = new();
    }
}
