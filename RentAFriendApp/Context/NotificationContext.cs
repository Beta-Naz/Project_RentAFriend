using Newtonsoft.Json;
using RentAFriendApp.Models.ClassesDTO.NotificationDTO;
using RentAFriendApp.Models.ClassesDTO.NotificationDTO.Response;
using System.Net;
using System.Net.Http;

namespace RentAFriendApp.Context
{
    class NotificationContext
    {
        private static readonly string _url = "https://localhost:7091/notification";

        /// <summary>
        /// Получить все уведомления пользователя
        /// </summary>
        public static async Task<NotificationsResponse?> GetMyNotifications(string token,
            int page = 1, int pageSize = 20, bool onlyUnread = false)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getAll?page={page}&pageSize={pageSize}&onlyUnread={onlyUnread}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<NotificationsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить непрочитанные уведомления
        /// </summary>
        public static async Task<UnreadNotificationsResponse?> GetUnreadNotifications(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/unread");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UnreadNotificationsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Отметить уведомление как прочитанное
        /// </summary>
        public static async Task<MarkAsReadResponse?> MarkAsRead(string token, int notificationId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/markAsRead/{notificationId}", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MarkAsReadResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Отметить все уведомления как прочитанные
        /// </summary>
        public static async Task<MarkAllAsReadResponse?> MarkAllAsRead(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/markAllAsRead", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MarkAllAsReadResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Создать уведомление (для администратора или системы)
        /// </summary>
        public static async Task<CreateNotificationResponse?> CreateNotification(string token, CreateNotificationDTO notificationData)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(notificationData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_url}/create", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CreateNotificationResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Удалить уведомление
        /// </summary>
        public static async Task<DeleteNotificationResponse?> DeleteNotification(string token, int notificationId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.DeleteAsync($"{_url}/delete/{notificationId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DeleteNotificationResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Удалить все уведомления пользователя
        /// </summary>
        public static async Task<DeleteAllNotificationsResponse?> DeleteAllNotifications(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.DeleteAsync($"{_url}/deleteAll");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DeleteAllNotificationsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить количество непрочитанных уведомлений
        /// </summary>
        public static async Task<UnreadCountResponse?> GetUnreadCount(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/unreadCount");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UnreadCountResponse>(result);
            }
            return null;
        }
    }
}