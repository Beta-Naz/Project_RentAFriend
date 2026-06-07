using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;

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

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GetAllProfilesResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить свой профиль
        /// </summary>
        public static async Task<GetMyProfileResponse?> GetMyProfile(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/myProfile");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GetMyProfileResponse>(result);
            }
            return null;
        }
    }
}