using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO.Response;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;


namespace RentAFriendApp.Context
{
    class ReviewContext
    {
        private static readonly string _url = "https://localhost:7091/review";

        /// <summary>
        /// Создать отзыв на бронирование (только клиент)
        /// </summary>
        public static async Task<CreateReviewResponse?> CreateReview(string token, CreateReviewDTO reviewData)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var content = new StringContent(JsonConvert.SerializeObject(reviewData),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_url}/create", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CreateReviewResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить отзывы о друге
        /// </summary>
        public static async Task<ReviewsByFriendResponse?> GetReviewsByFriend(int friendProfileId, string token,
            int page = 1, int pageSize = 10, bool onlyApproved = true)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/getByFriend/{friendProfileId}?page={page}&pageSize={pageSize}&onlyApproved={onlyApproved}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ReviewsByFriendResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить свои отзывы (как клиент)
        /// </summary>
        public static async Task<MyReviewsResponse?> GetMyReviews(string token, int page = 1, int pageSize = 10)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/myReviews?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MyReviewsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Получить отзывы на модерации (только администратор)
        /// </summary>
        public static async Task<PendingReviewsResponse?> GetPendingReviews(string token, int page = 1, int pageSize = 20)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.GetAsync($"{_url}/pending?page={page}&pageSize={pageSize}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PendingReviewsResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Одобрить отзыв (только администратор)
        /// </summary>
        public static async Task<ApproveReviewResponse?> ApproveReview(string token, int reviewId, string? moderatorComment = null)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var data = new { moderatorComment };
            var content = new StringContent(JsonConvert.SerializeObject(data),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{_url}/approve/{reviewId}", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApproveReviewResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Отклонить отзыв (только администратор)
        /// </summary>
        public static async Task<RejectReviewResponse?> RejectReview(string token, int reviewId, string? moderatorComment = null)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var data = new { moderatorComment };
            var content = new StringContent(JsonConvert.SerializeObject(data),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{_url}/reject/{reviewId}", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RejectReviewResponse>(result);
            }
            return null;
        }

        /// <summary>
        /// Удалить отзыв (администратор или автор)
        /// </summary>
        public static async Task<DeleteReviewResponse?> DeleteReview(string token, int reviewId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("TOKEN", token);
            var response = await client.DeleteAsync($"{_url}/delete/{reviewId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DeleteReviewResponse>(result);
            }
            return null;
        }
    }
}