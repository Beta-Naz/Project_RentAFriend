using System.Windows.Media;

namespace RentAFriendApp.Models.ClassesDTO.ChatDTO
{
    public class ChatListDTO
    {
        // === Существующие свойства (оставьте как есть) ===
        public int ChatID { get; set; }
        public int InterlocutorID { get; set; }
        public string InterlocutorName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // === НОВЫЕ вычисляемые свойства для UI ===

        // Алиас для совместимости с XAML
        public string UserName => InterlocutorName;

        // Алиас для совместимости с XAML
        public int ClientID => InterlocutorID;

        // Инициалы для аватарки
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(InterlocutorName))
                    return "??";

                var parts = InterlocutorName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();

                return InterlocutorName.Length >= 2
                    ? InterlocutorName.Substring(0, 2).ToUpper()
                    : InterlocutorName.ToUpper();
            }
        }

        // Цвет фона аватарки (генерируется из имени для разнообразия)
        public Brush AvatarBackground
        {
            get
            {
                // Палитра приятных пастельных цветов
                var colors = new[]
                {
                    Color.FromRgb(232, 245, 233), // светло-зелёный
                    Color.FromRgb(227, 242, 253), // светло-синий
                    Color.FromRgb(255, 243, 224), // светло-оранжевый
                    Color.FromRgb(243, 229, 245), // светло-фиолетовый
                    Color.FromRgb(255, 236, 236), // светло-розовый
                    Color.FromRgb(232, 245, 245), // светло-бирюзовый
                };

                if (string.IsNullOrWhiteSpace(InterlocutorName))
                    return new SolidColorBrush(colors[0]);

                // Детерминированный выбор цвета по имени
                int hash = Math.Abs(InterlocutorName.GetHashCode());
                return new SolidColorBrush(colors[hash % colors.Length]);
            }
        }

        // Форматированное время последнего сообщения
        public string LastMessageTimeDisplay
        {
            get
            {
                if (LastMessageAt == null)
                    return "";

                var date = LastMessageAt.Value;
                var now = DateTime.Now;

                if (date.Date == now.Date)
                    return date.ToString("HH:mm");

                if (date.Date == now.Date.AddDays(-1))
                    return "Вчера";

                if (date.Year == now.Year)
                    return date.ToString("dd MMM");

                return date.ToString("dd.MM.yyyy");
            }
        }

        // Есть ли непрочитанные сообщения
        public bool HasUnreadMessages => UnreadCount > 0;
    }
}