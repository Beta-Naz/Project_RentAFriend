namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class PayBookingResponse
    {
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
    }
}
