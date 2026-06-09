namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO.Response
{
    public class ScheduleDateResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<ScheduleDTO>? Slots { get; set; }
    }
}
