namespace RentAFriendApp.Models
{
    public class BookingStatistics
    {
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageCheck { get; set; }
    }
}
