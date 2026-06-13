namespace RentAFriendApp.Models.ClassesDTO.MessageDTO.Response
{
    public class LastMessageItem
    {
        public int MessageID { get; set; }
        public int ChatID { get; set; }
        public int SenderID { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string MessageType { get; set; } = "Text";

        public DateTime CreatedAt;
    }
}
