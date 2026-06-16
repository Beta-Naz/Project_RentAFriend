using Newtonsoft.Json;
using RentAFriendApp.Classes;
using RentAFriendApp.Models.ClassesDTO.ChatDTO.Response;
using System.Net;
using System.Net.Http;

namespace RentAFriendApp.Context
{
    class ChatContext
    {
        private static readonly string _url = Config.URL + "chat";

        /// <summary>
        /// Получить или создать чат с другом
        /// </summary>
        public static async Task<GetOrCreateChatResponse?> GetOrCreateChat(string token, int friendId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PostAsync($"{_url}/getOrCreate/{friendId}", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GetOrCreateChatResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить все чаты пользователя
        /// </summary>
        public static async Task<MyChatsResponse?> GetMyChats(string token, int page = 1, int pageSize = 20)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/myChats?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MyChatsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить детали чата
        /// </summary>
        public static async Task<ChatDetailsResponse?> GetChatDetails(string token, int chatId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/{chatId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ChatDetailsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Закрыть чат
        /// </summary>
        public static async Task<CloseChatResponse?> CloseChat(string token, int chatId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/{chatId}/close", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CloseChatResponse>(result);
            }
            return null;
        }
    }
}