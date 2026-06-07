namespace RentAFriendApp.Models.ClassesDTO.NotificationDTO.Response
{
    public class UnreadNotificationsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<NotificationDTO> Notifications { get; set; } = [];
    }
}
