using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using RentAFriendApp.Models.ClassesDTO.AuditLogDTO;
using RentAFriendApp.Models.ClassesDTO.AuditLogDTO.Response;

namespace RentAFriendApp.Context
{
    class AuditLogContext
    {
        private static readonly string _url = "https://localhost:7091/audit";

        /// <summary>
        /// Получить все логи (только для администратора)
        /// </summary>
        public static async Task<GetAllLogsResponse?> GetAllLogs(string token, int page = 1, int pageSize = 50)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getAll?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GetAllLogsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить статистику по логам (только для администратора)
        /// </summary>
        public static async Task<LogStatisticsResponse?> GetLogStatistics(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/statistics");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<LogStatisticsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Создать свой лог (для любого пользователя)
        /// </summary>
        public static async Task<CreateLogResponse?> CreateLog(string token, CreateLogDTO logData)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(logData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_url}/create", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CreateLogResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Удалить все логи (только для администратора)
        /// </summary>
        public static async Task<DeleteAllLogsResponse?> DeleteAllLogs(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.DeleteAsync($"{_url}/deleteAll");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DeleteAllLogsResponse>(result);
            }
            return null;
        }
    }
}