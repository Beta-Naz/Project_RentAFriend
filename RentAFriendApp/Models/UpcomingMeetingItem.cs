namespace RentAFriendApp.Models
{
    public class UpcomingMeetingItem
    {
        public int BookingID { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? MeetingLocation { get; set; }
        public string TimeRangeDisplay => $"{StartTime:hh\\:mm} – {EndTime:hh\\:mm}";
    }
}
