using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.ScheduleDTO;

namespace RentAFriendApp.Context
{
    class ScheduleContext
    {
        private static readonly string _url = "https://localhost:7091/schedule";

        /// <summary>
        /// Получить расписание на конкретную дату
        /// </summary>
        public static async Task<ScheduleDTO?> GetScheduleByDate(int profileId, DateTime date, string token)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var response = await client.GetAsync($"{_url}/getByDate/{profileId}?date={date:yyyy-MM-dd}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ScheduleDTO>(result);
                }
                return null;
            }
        }

        /// <summary>
        /// Получить доступные слоты для бронирования
        /// </summary>
        public static async Task<AvailableSlotsResponse?> GetAvailableTimeSlots(int profileId, DateTime date, string token)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var response = await client.GetAsync($"{_url}/available/{profileId}?date={date:yyyy-MM-dd}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<AvailableSlotsResponse>(result);
                }
                return null;
            }
        }

        /// <summary>
        /// Создать временной слот (для друга)
        /// </summary>
        public static async Task<CreateScheduleDTO?> CreateTimeSlot(string token, CreateScheduleDTO scheduleData)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var content = new StringContent(JsonConvert.SerializeObject(scheduleData),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_url}/create", content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CreateScheduleDTO>(result);
                }
                return null;
            }
        }

        /// <summary>
        /// Удалить временной слот (для друга)
        /// </summary>
        public static async Task<bool> DeleteTimeSlot(string token, int scheduleId)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var response = await client.DeleteAsync($"{_url}/delete/{scheduleId}");
                return response.StatusCode == HttpStatusCode.OK;
            }
        }

        /// <summary>
        /// Обновить доступность слота
        /// </summary>
        public static async Task<UpdateAvailabilityDTO?> UpdateTimeSlotAvailability(string token, int scheduleId, bool isAvailable)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var data = new { isAvailable = isAvailable };
                var content = new StringContent(JsonConvert.SerializeObject(data),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{_url}/updateAvailability/{scheduleId}", content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<UpdateAvailabilityDTO>(result);
                }
                return null;
            }
        }

        /// <summary>
        /// Очистить расписание на дату
        /// </summary>
        public static async Task<ClearDateDTO?> ClearScheduleForDate(string token, DateTime date)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var response = await client.DeleteAsync($"{_url}/clearDate?date={date:yyyy-MM-dd}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ClearDateDTO>(result);
                }
                return null;
            }
        }

        /// <summary>
        /// Создать стандартное недельное расписание
        /// </summary>
        public static async Task<CreateWeekScheduleResponse?> CreateDefaultWeekSchedule(string token, DateTime startDate)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var data = new { startDate = startDate };
                var content = new StringContent(JsonConvert.SerializeObject(data),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_url}/createWeekSchedule", content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CreateWeekScheduleResponse>(result);
                }
                return null;
            }
        }

        /// <summary>
        /// Проверить пересечение временных слотов
        /// </summary>
        public static async Task<bool?> CheckTimeSlotOverlap(string token, CheckOverlapDTO overlapData)
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("TOKEN", token);
                var content = new StringContent(JsonConvert.SerializeObject(overlapData),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_url}/checkOverlap", content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    BoolResult? boolResult = JsonConvert.DeserializeObject<BoolResult>(result);
                    if(boolResult != null)
                    {
                        return boolResult.Result;
                    }
                }
                return null;
            }
        }
    }
}