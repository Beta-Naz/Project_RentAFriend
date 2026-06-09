namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class UpdateBookingStatusResponse
    {
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
