namespace RentAFriendApp.Models.ClassesDTO.AuditLogDTO.Response
{
    public class GetMyLogsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<AuditLogDTO> Logs { get; set; } = new();
    }
}
