namespace Project_RentAFriend.Models.ClassesDTO.ChatDTO
{
    public class ChatListDTO
    {
        public int ChatID { get; set; }
        public int InterlocutorID { get; set; }
        public string InterlocutorName { get; set; } = string.Empty;
        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}