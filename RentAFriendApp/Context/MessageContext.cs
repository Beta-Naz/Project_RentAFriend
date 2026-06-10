using Newtonsoft.Json;
using RentAFriendApp.Models.ClassesDTO.MessageDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO.Response;
using System.Net;
using System.Net.Http;

namespace RentAFriendApp.Context
{
    class MessageContext
    {
        private static readonly string _url = "https://localhost:7091/message";

        /// <summary>
        /// Получить сообщения чата
        /// </summary>
        public static async Task<MessagesResponse?> GetMessages(string token, int chatId,
            int page = 1, int pageSize = 50)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getByChat/{chatId}?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MessagesResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        public static async Task<SendMessageResponse?> SendMessage(string token, SendMessageDTO messageData)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(messageData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_url}/send", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SendMessageResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Редактировать сообщение
        /// </summary>
        public static async Task<EditMessageResponse?> EditMessage(string token, int messageId, string newContent)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var data = new { content = newContent };
            var content = new StringContent(JsonConvert.SerializeObject(data),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{_url}/edit/{messageId}", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<EditMessageResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Удалить сообщение (мягкое удаление)
        /// </summary>
        public static async Task<DeleteMessageResponse?> DeleteMessage(string token, int messageId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.DeleteAsync($"{_url}/delete/{messageId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DeleteMessageResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить количество непрочитанных сообщений
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

        /// <summary>
        /// Пометить сообщения как прочитанные
        /// </summary>
        public static async Task<MarkAsReadResponse?> MarkMessagesAsRead(string token, int chatId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/markAsRead/{chatId}", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MarkAsReadResponse>(result);
            }
            return null;
        }
        public static async Task<LastMessagesResponse?> GetRecentMessages(string token, int count = 50)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/recent?count={count}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<LastMessagesResponse>(result);
            }
            return null;
        }
    }
}