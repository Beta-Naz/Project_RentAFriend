namespace RentAFriendApp.Models.ClassesDTO.AuditLogDTO.Response
{
    public class GetAllLogsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<AuditLogDTO> Logs { get; set; } = [];
    }
}
