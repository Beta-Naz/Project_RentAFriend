namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class UpcomingBookingsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<UpcomingBookingItem> Bookings { get; set; } = [];
    }
}
