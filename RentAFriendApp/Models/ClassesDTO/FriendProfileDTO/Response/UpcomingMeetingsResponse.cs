using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class UpcomingMeetingsResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; } = false;
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        [JsonProperty("count")]
        public int Count { get; set; }
        [JsonProperty("meetings")]
        public List<UpcomingMeetingItem> Meetings { get; set; } = new();
    }
}
