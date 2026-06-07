namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class BookingHistoryResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<BookingHistoryItem> Bookings { get; set; } = [];
    }
}
