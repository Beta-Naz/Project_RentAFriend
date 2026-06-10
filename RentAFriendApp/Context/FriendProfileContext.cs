using Newtonsoft.Json;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;
using System.Net;
using System.Net.Http;
using System.Text;

namespace RentAFriendApp.Context
{
    class FriendProfileContext
    {
        private static readonly string _url = "https://localhost:7091/friend";

        /// <summary>
        /// Создать профиль друга
        /// </summary>
        public static async Task<CreateProfileResponse?> CreateProfile(string token, FPMainInfoDTO infoDTO)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(infoDTO),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_url}/create", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CreateProfileResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Обновить профиль друга
        /// </summary>
        public static async Task<UpdateProfileResponse?> UpdateProfile(string token, FPMainInfoDTO infoDTO)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(infoDTO),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{_url}/update", content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UpdateProfileResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить все профили друзей
        /// </summary>
        public static async Task<GetAllProfilesResponse?> GetAllProfiles(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getAll");

            string result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GetAllProfilesResponse>(result);
        }

        /// <summary>
        /// Получить свой профиль
        /// </summary>
        public static async Task<GetMyProfileResponse?> GetMyProfile(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/myProfile");
            string result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GetMyProfileResponse>(result);
        }
        /// <summary>
        /// Получить профиль друга по ID
        /// </summary>
        public static async Task<GetProfileResponse?> GetFriendProfileById(int profileId, string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/profile/{profileId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"JSON ОТ СЕРВЕРА: {result}");

                return JsonConvert.DeserializeObject<GetProfileResponse>(result);
            }
            return null;
        }
        /// <summary>
        /// Получить статистику профиля друга
        /// </summary>
        public static async Task<FPStatsDTO?> GetFriendProfileStats(string token, int profileId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/stats/{profileId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<FPStatsDTO>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить ближайшие встречи друга
        /// </summary>
        public static async Task<UpcomingMeetingsResponse?> GetUpcomingMeetings(string token, int profileId, int top = 5)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/upcomingMeetings/{profileId}?top={top}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UpcomingMeetingsResponse>(result);
            }
            return null;
        }
        /// <summary>
        /// Получить список доступных городов для фильтрации и автодополнения
        /// </summary>
        public static async Task<List<string>?> GetAvailableCities(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/cities");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<string>>(result);
            }
            return null;
        }
        public static async Task<BoolResult?> VerifyFriendProfile(string token, int profileId, bool isVerified)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var data = new { isVerified };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{_url}/verify/{profileId}", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BoolResult>(result);
            }
            return null;
        }
    }

}