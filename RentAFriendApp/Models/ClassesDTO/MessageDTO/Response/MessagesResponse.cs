namespace RentAFriendApp.Models.ClassesDTO.MessageDTO.Response
{
    public class MessagesResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<MessageDTO> Messages { get; set; } = [];
    }
}
