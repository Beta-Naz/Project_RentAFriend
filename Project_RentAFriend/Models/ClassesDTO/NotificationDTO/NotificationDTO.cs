namespace Project_RentAFriend.Models.ClassesDTO.NotificationDTO
{
    public class NotificationDTO
    {
        public int NotificationID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public static NotificationDTO Convert(Notification notification)
        {
            return new NotificationDTO
            {
                NotificationID = notification.NotificationID,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}