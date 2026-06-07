namespace RentAFriendApp.Models.ClassesDTO.ChatDTO.Response
{
    public class MyChatsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatListDTO> Chats { get; set; } = [];
    }
}
