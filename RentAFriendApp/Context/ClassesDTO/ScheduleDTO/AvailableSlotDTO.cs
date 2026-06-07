namespace Project_RentAFriend.Models.ClassesDTO.ScheduleDTO
{
    public class AvailableSlotDTO
    {
        public int ScheduleID { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
