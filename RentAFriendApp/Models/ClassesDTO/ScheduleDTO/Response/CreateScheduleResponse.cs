namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO
{
    public class CreateScheduleResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public ScheduleDTO? Slot { get; set; }
    }
}
