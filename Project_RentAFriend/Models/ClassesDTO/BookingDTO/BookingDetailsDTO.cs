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
        public int ClientID { get; set; }
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

        public static BookingDetailsDTO Convert(Booking booking)
        {
            return new BookingDetailsDTO
            {
                BookingID = booking.BookingID,
                Status = booking.Status,
                Purpose = booking.Purpose,
                TotalAmount = booking.TotalAmount,
                PaymentStatus = booking.PaymentStatus,
                PaymentDate = booking.PaymentDate,
                MeetingLocation = booking.MeetingLocation,
                SpecialRequests = booking.SpecialRequests,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                ClientName = booking.Client?.FullName ?? "Unknown",
                ClientEmail = booking.Client?.Email ?? "Unknown",
                ClientPhone = booking.Client?.Phone ?? "Unknown",
                FriendName = booking.FriendProfile?.User?.FullName ?? "Unknown",
                FriendCity = booking.FriendProfile?.City ?? "Unknown",
                FriendHourlyRate = booking.FriendProfile?.HourlyRate,
                ScheduleDate = booking.Schedule?.Date ?? DateTime.MinValue,
                StartTime = booking.Schedule?.StartTime ?? TimeSpan.Zero,
                EndTime = booking.Schedule?.EndTime ?? TimeSpan.Zero,
                HasReview = booking.Review != null,
                ReviewRating = booking.Review?.Rating,
                ReviewComment = booking.Review?.Comment
            };
        }
    }
}