namespace Project_RentAFriend.Models.ClassesDTO.BookingDTO
{
    public class ClientBookingDTO
    {
        public int BookingID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? MeetingLocation { get; set; }
        public DateTime CreatedAt { get; set; }
        public int FriendId { get; set; }
        public string FriendName { get; set; } = string.Empty;
        public string FriendCity { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool HasReview { get; set; }

        public static ClientBookingDTO Convert(Booking booking)
        {
            return new ClientBookingDTO
            {
                BookingID = booking.BookingID,
                Status = booking.Status,
                Purpose = booking.Purpose,
                TotalAmount = booking.TotalAmount,
                PaymentStatus = booking.PaymentStatus,
                MeetingLocation = booking.MeetingLocation,
                CreatedAt = booking.CreatedAt,
                FriendName = booking.FriendProfile?.User?.FullName ?? "Unknown",
                FriendCity = booking.FriendProfile?.City ?? "Unknown",
                FriendId = booking.FriendProfile?.ProfileID ?? -1,
                ScheduleDate = booking.Schedule?.Date ?? DateTime.MinValue,
                StartTime = booking.Schedule?.StartTime ?? TimeSpan.Zero,
                EndTime = booking.Schedule?.EndTime ?? TimeSpan.Zero,
                HasReview = booking.Review != null
            };
        }
    }
}