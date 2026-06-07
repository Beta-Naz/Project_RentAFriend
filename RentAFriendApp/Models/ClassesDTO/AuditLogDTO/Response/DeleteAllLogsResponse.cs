namespace RentAFriendApp.Models.ClassesDTO.AuditLogDTO.Response
{
    public class DeleteAllLogsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int DeletedCount { get; set; }
    }
}
