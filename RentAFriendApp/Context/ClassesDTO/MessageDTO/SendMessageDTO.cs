namespace Project_RentAFriend.Models.ClassesDTO.MessageDTO
{
    public class SendMessageDTO
    {
        public int ChatID { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? MessageType { get; set; } = "Text";
    }
}
