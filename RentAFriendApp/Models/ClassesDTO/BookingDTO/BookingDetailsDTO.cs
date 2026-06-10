using System.Windows.Media;

namespace RentAFriendApp.Models.ClassesDTO.BookingDTO
{
    public class BookingDetailsDTO
    {
        public int BookingID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public string? MeetingLocation { get; set; }
        public string? SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Client info
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        // Friend info
        public string FriendName { get; set; } = string.Empty;
        public string FriendCity { get; set; } = string.Empty;
        public decimal? FriendHourlyRate { get; set; }

        // Schedule info
        public DateTime ScheduleDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool HasReview { get; set; }
        public int? ReviewRating { get; set; }
        public string? ReviewComment { get; set; }
        
        /// <summary>
        /// Для работы.
        /// </summary>
        
        public string ClientInitials => string.IsNullOrWhiteSpace(ClientName)
        ? "??"
        : string.Concat(ClientName.Split(' ').Take(2).Select(s => s[0])).ToUpper();

        public DateTime Date => ScheduleDate;
        public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

        public string StatusDisplay => Status switch
        {
            "Pending" => "Ожидает",
            "Confirmed" => "Подтверждено",
            "Completed" => "Завершено",
            "Cancelled" => "Отменено",
            "Rejected" => "Отклонено",
            _ => Status
        };

        public string PaymentStatusDisplay => PaymentStatus switch
        {
            "Paid" => "Оплачено",
            "Unpaid" => "Не оплачено",
            "Refunded" => "Возвращено",
            _ => PaymentStatus
        };

        public Brush PaymentStatusBrush => PaymentStatus switch
        {
            "Paid" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            "Unpaid" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
        };
    }
}