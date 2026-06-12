using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetMyProfileResponse
    {
        [JsonProperty("profile")]
        public FPInfoDTO Profile { get; set; } = new();

        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
