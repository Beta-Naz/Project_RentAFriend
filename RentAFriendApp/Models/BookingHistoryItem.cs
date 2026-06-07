namespace RentAFriendApp.Models
{
    public class BookingHistoryItem
    {
        public int BookingID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? MeetingLocation { get; set; }
        public string FriendName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasReview { get; set; }
    }
}
