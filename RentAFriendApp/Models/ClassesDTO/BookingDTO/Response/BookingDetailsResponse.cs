namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class BookingDetailsResponse
    {
        public string Message { get; set; } = string.Empty;
        public BookingDetailsDTO Booking { get; set; } = new();
    }
}
