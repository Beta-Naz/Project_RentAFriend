namespace RentAFriendApp.Models.ClassesDTO.MessageDTO.Response
{
    public class EditMessageResponse
    {
        public string Message { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public string NewContent { get; set; } = string.Empty;
    }
}
