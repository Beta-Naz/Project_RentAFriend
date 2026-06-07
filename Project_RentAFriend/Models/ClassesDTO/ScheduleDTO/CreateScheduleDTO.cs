namespace Project_RentAFriend.Models.ClassesDTO.ScheduleDTO
{
    public class CreateScheduleDTO
    {
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
