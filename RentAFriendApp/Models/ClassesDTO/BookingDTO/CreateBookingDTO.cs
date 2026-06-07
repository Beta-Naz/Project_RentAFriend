namespace RentAFriendApp.Models.ClassesDTO.BookingDTO
{
    public class CreateBookingDTO
    {
        public int ScheduleID { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string? MeetingLocation { get; set; }
        public string? SpecialRequests { get; set; }
    }
}