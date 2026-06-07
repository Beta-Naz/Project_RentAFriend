namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO
{
    public class CreateWeekScheduleResponse
    {
        public string Message { get; set; } = string.Empty;
        public int SlotsCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
