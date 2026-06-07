namespace Project_RentAFriend.Models.ClassesDTO.BookingDTO
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
    }
}