namespace RentAFriendApp.Models.ClassesDTO.BookingDTO.Response
{
    public class BookingStatisticsResponse
    {
        public string Message { get; set; } = string.Empty;
        public BookingStatistics Statistics { get; set; } = new();
    }
}
