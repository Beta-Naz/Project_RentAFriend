namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO
{
    public class ScheduleDTO
    {
        public int ScheduleID { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsBooked { get; set; }
        public int? BookingID { get; set; }
    }
}
