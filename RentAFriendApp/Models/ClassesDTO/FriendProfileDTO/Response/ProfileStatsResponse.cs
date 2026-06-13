using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class ProfileStatsResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("statistics")]
        public FPStatsDTO Statistic { get; set; }
    }
}
