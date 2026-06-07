namespace RentAFriendApp.Models.ClassesDTO.NotificationDTO.Response
{
    public class CreateNotificationResponse
    {
        public string Message { get; set; } = string.Empty;
        public int NotificationId { get; set; }
        public NotificationDTO Notification { get; set; } = new();
    }
}
