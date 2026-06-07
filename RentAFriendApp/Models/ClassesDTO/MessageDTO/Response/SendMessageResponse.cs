namespace RentAFriendApp.Models.ClassesDTO.MessageDTO.Response
{
    public class SendMessageResponse
    {
        public string Message { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public DateTime SentAt { get; set; }
    }
}
