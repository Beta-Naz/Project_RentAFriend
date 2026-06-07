namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class CreateBookingResponse
    {
        public string Message { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public decimal TotalAmount { get; set; }
        public ScheduleInfo Schedule { get; set; } = new();
    }
}
