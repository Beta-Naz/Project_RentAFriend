using Newtonsoft.Json;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.UserDTO;
using RentAFriendApp.Models.ClassesDTO.UserDTO.Response;
using System.Net;
using System.Net.Http;
using System.Text;

namespace RentAFriendApp.Context
{
    class UserContext
    {
        private static readonly string _url = "https://localhost:7091/user";
        public static async Task<Auth?> Login(string email , string password)
        {
            using HttpClient client = new();
            using HttpRequestMessage request = new(HttpMethod.Post, _url + "/login");
            Dictionary<string, string> formData = new()
            {
                ["email"] = email,
                ["password"] = password
            };
            FormUrlEncodedContent content = new(formData);
            request.Content = content;
            var response = await client.SendAsync(request);
            string result = await response.Content.ReadAsStringAsync();
            Auth? dataAuth = JsonConvert.DeserializeObject<Auth>(result);
            if (dataAuth != null)
            {
                return dataAuth;
            }
            return null;
        }
        public static async Task<UserLoginDTO?> GetUser(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync(_url + "/get");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserLoginDTO>(result);
            }
            return null;
        }
        public static async Task<bool> Register(UserRegisterDTO registerData)
        {
            using HttpClient client = new();
            var content = new StringContent(JsonConvert.SerializeObject(registerData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_url + "/create", content);
            return response.StatusCode == HttpStatusCode.OK;
        }
        public static async Task<bool> ExistsEmail(string email)
        {
            using HttpClient client = new();
            var formData = new Dictionary<string, string> { ["email"] = email };
            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(_url + "/existsEmail", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<BoolResult>(result);
                return data?.Result ?? false;
            }
            return false;
        }
        public static async Task<bool> UpdateUser(string token, UserMainInfoDTO updateData)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(updateData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync(_url + "/update", content);
            return response.StatusCode == HttpStatusCode.OK;
        }
        // Получить всех пользователей
        public static async Task<GetAllUsersResponse?> GetAllUsers(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getAll");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GetAllUsersResponse>(result);
            }
            return null;
        }

        // Обновить статус пользователя (блокировка/разблокировка)
        public static async Task<BoolResult?> UpdateUserStatus(string token, int userId, bool isActive)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var dataForm = new Dictionary<string, string> { ["isActive"] = isActive ? "true" : "false"};
            var content = new FormUrlEncodedContent(dataForm);
            var response = await client.PutAsync($"{_url}/updateStatus/{userId}", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BoolResult>(result);
            }
            return null;
        }

        // Удалить пользователя
        public static async Task<BoolResult?> DeleteUser(string token, int userId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.DeleteAsync($"{_url}/delete/{userId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BoolResult>(result);
            }
            return null;
        }
        public static async Task<bool> Logout(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PostAsync($"{_url}/logout", null);
            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}
