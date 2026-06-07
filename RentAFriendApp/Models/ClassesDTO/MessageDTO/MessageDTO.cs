namespace RentAFriendApp.Models.ClassesDTO.MessageDTO
{
    public class MessageDTO
    {
        public int MessageID { get; set; }
        public int SenderID { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}