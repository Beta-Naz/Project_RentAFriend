namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO
{
    public class CreateScheduleResponse
    {
        public int ScheduleID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }
}
