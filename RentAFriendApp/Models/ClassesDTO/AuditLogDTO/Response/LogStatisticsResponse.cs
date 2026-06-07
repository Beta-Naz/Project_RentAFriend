using RentAFriendApp.Models.ClassesDTO.AuditLogDTO.Response.Stat;

namespace RentAFriendApp.Models.ClassesDTO.AuditLogDTO.Response
{
    public class LogStatisticsResponse
    {
        public string Message { get; set; } = string.Empty;
        public int TotalLogs { get; set; }
        public List<ActionStat> TopActions { get; set; } = [];
        public List<DailyStat> DailyStats { get; set; } = [];
        public List<TableStat> TopTables { get; set; } = [];
    }

}
