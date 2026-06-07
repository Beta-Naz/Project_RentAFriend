namespace RentAFriendApp.Models.ClassesDTO.BookingDTO
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
        public string FriendName { get; set; } = string.Empty;
        public string FriendCity { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool HasReview { get; set; }
    }
}