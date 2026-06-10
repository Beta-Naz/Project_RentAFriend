using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.Models.ClassesDTO.BookingDTO.Response;

namespace RentAFriendApp.Context
{
    class BookingContext
    {
        private static readonly string _url = "https://localhost:7091/booking";

        /// <summary>
        /// Создать новое бронирование
        /// </summary>
        public static async Task<CreateBookingResponse?> CreateBooking(string token, CreateBookingDTO bookingData)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(bookingData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_url}/create", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CreateBookingResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить мои бронирования (как клиент)
        /// </summary>
        public static async Task<MyBookingsResponse?> GetMyBookings(string token,
            string? status = null, int page = 1, int pageSize = 10)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var url = $"{_url}/myBookings?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MyBookingsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить детали бронирования
        /// </summary>
        public static async Task<BookingDetailsResponse?> GetBookingDetails(string token, int bookingId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/{bookingId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BookingDetailsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Отменить бронирование (клиент)
        /// </summary>
        public static async Task<CancelBookingResponse?> CancelBooking(string token, int bookingId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/cancel/{bookingId}", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CancelBookingResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Оплатить бронирование (клиент)
        /// </summary>
        public static async Task<PayBookingResponse?> PayBooking(string token, int bookingId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/pay/{bookingId}", null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PayBookingResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить ближайшие бронирования (клиент)
        /// </summary>
        public static async Task<UpcomingBookingsResponse?> GetUpcomingBookings(string token, int top = 5)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/upcoming?top={top}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UpcomingBookingsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить историю бронирований (клиент)
        /// </summary>
        public static async Task<BookingHistoryResponse?> GetBookingHistory(string token, int page = 1, int pageSize = 10)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/history?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BookingHistoryResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить статистику по бронированиям (клиент)
        /// </summary>
        public static async Task<BookingStatisticsResponse?> GetBookingStatistics(string token)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/statistics");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BookingStatisticsResponse>(result);
            }
            return null;
        }
        /// <summary>
        /// Обновить статус бронирования
        /// </summary>
        public static async Task<UpdateBookingStatusResponse?> UpdateBookingStatus(string token, int bookingId, string newStatus)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var formData = new Dictionary<string, string>
            {
                ["newStatus"] = newStatus
            };
            var content = new FormUrlEncodedContent(formData);
            var response = await client.PutAsync($"{_url}/updateStatus/{bookingId}", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UpdateBookingStatusResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Отклонить бронирование
        /// </summary>
        public static async Task<RejectBookingResponse?> RejectBooking(string token, int bookingId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.PutAsync($"{_url}/reject/{bookingId}",null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RejectBookingResponse>(result);
            }
            return null;
        }
        /// <summary>
        /// Получить бронирования друга
        /// </summary>
        public static async Task<FriendBookingsResponse?> GetFriendBookings(string token, int profileId, string? statusFilter = null)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);

            string url = $"{_url}/friendBookings/{profileId}";
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Все")
            {
                url += $"?status={statusFilter}";
            }

            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<FriendBookingsResponse>(result);
            }
            return null;
        }
        public static async Task<AllBookingsResponse?> GetAllBookings(string token, int page = 1, int pageSize = 50)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getAll?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AllBookingsResponse>(result);
            }
            return null;
        }
    }
}