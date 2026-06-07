namespace RentAFriendApp.Models.ClassesDTO.ChatDTO.Response
{
    public class GetOrCreateChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public int ChatId { get; set; }
        public InterlocutorInfo Interlocutor { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
