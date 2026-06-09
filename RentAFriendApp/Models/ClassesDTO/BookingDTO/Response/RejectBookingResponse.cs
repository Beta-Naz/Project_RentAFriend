namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class RejectBookingResponse
    {
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
