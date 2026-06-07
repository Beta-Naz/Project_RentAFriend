namespace RentAFriendApp.Models.ClassesDTO.AuditLogDTO
{
    public class CreateLogDTO
    {
        public string Action { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int RecordID { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}