namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class MyBookingsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<ClientBookingDTO> Bookings { get; set; } = [];
    }
}
