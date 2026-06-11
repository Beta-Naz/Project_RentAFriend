namespace RentAFriendApp.Models.ClassesDTO.MessageDTO
{
    public class MessageDTO
    {
        // === Существующие свойства ===
        public int MessageID { get; set; }
        public int ChatID { get; set; }
        public int SenderID { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }

        private static int _currentUserId;
        public static void SetCurrentUserId(int userId) => _currentUserId = userId;

        // Исходящее ли сообщение
        public bool IsOutgoing => SenderID == _currentUserId;

        // Алиас для совместимости с XAML
        public string Text => Content;

        // Форматированное время
        public string TimeDisplay
        {
            get
            {
                var now = DateTime.Now;
                if (CreatedAt.Date == now.Date)
                    return CreatedAt.ToString("HH:mm");

                if (CreatedAt.Date == now.Date.AddDays(-1))
                    return $"Вчера, {CreatedAt:HH:mm}";

                return CreatedAt.ToString("dd.MM HH:mm");
            }
        }
    }
}