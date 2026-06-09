namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class UpcomingMeetingsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<UpcomingMeetingItem> Meetings { get; set; } = new();
    }
}
