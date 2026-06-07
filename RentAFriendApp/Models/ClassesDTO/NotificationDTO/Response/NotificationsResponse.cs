namespace RentAFriendApp.Models.ClassesDTO.NotificationDTO.Response
{
    public class NotificationsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public List<NotificationDTO> Notifications { get; set; } = new();
    }
}
