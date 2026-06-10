namespace RentAFriendApp.Models.ClassesDTO.MessageDTO.Response
{
    public class LastMessagesResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<LastMessageItem> Messages { get; set; } = [];
    }
}
