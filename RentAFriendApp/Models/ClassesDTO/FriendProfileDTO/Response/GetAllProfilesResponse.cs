using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetAllProfilesResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("profiles")]
        public List<FPInfoDTO> Profiles { get; set; } = new();
    }
}
