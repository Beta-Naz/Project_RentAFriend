using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace RentAFriendApp.Context
{
    class AdminContext
    {
        private static readonly string _url = "https://localhost:7091/admin";

        // Получить статистику
        public static async Task<AdminStatsDTO?> GetStatistics(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/statistics");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AdminStatsDTO>(result);
            }
            return null;
        }
    }

    public class AdminStatsDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BlockedUsers { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingVerifications { get; set; }
    }
}
