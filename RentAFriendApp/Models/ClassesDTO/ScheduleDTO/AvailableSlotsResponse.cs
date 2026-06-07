namespace RentAFriendApp.Models.ClassesDTO.ScheduleDTO
{
    public class AvailableSlotsResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public List<AvailableSlotDTO> Slots { get; set; } = new();
    }
}
