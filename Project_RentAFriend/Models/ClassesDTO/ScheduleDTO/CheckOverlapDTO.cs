namespace Project_RentAFriend.Models.ClassesDTO.ScheduleDTO
{
    public class CheckOverlapDTO
    {
        public int ProfileID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? ScheduleID { get; set; }
    }
}
