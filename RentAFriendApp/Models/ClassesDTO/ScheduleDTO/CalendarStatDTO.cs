namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO
{
    public class CalendarStatDTO
    {
        public DateTime Date { get; set; }
        public int SlotCount { get; set; }
        public int AvailableCount { get; set; }
        public int BookedCount { get; set; }

    }
}
