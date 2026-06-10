namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class AllBookingsResponse
    {
        public List<BookingDetailsDTO> Bookings { get; set; } = new();
    }
}
