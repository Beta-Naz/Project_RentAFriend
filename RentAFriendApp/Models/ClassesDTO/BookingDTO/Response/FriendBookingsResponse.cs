namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class FriendBookingsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<BookingDetailsDTO> Bookings { get; set; } = new();
    }
}
