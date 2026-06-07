namespace RentAFriendApp.Models.ClassesDTO.AuditLogDTO
{
    public class AuditLogDTO
    {
        public int LogID { get; set; }
        public int? UserID { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int RecordID { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}