namespace RentAFriendApp.Models
{
    public class ChatDetail
    {
        public int ChatID { get; set; }
        public InterlocutorInfo Interlocutor { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsActive { get; set; }
    }
}
