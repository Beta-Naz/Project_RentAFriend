namespace RentAFriendApp.Models.ClassesDTO.ChatDTO.Response
{
    public class ChatDetailsResponse
    {
        public string Message { get; set; } = string.Empty;
        public ChatDetail Chat { get; set; } = new();
    }
}
