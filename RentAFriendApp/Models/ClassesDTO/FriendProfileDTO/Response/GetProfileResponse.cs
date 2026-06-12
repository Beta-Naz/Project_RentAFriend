using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetProfileResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("profile")]
        public FPInfoDTO Profile { get; set; }
    }
}
