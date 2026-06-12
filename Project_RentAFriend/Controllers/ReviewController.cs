using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.ReviewDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/review")]
    public class ReviewController : Controller
    {
        private readonly DBManager? _dbManager;

        public ReviewController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Создать отзыв на бронирование (только клиент)
        /// </summary>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> CreateReview(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CreateReviewDTO reviewData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Bookings == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Проверяем существование бронирования
                var booking = await _dbManager.Bookings
                    .Include(b => b.FriendProfile)
                    .FirstOrDefaultAsync(b => b.BookingID == reviewData.BookingID);

                if (booking == null)
                {
                    return NotFound(new { message = "Бронирование не найдено" });
                }

                // Проверяем, что клиент оставляет отзыв
                if (booking.ClientID != userId)
                {
                    return Forbid("Только клиент может оставить отзыв на это бронирование");
                }

                // Проверяем статус бронирования (только завершенные)
                if (booking.Status != "Completed")
                {
                    return BadRequest(new { message = "Отзыв можно оставить только после завершения встречи" });
                }

                // Проверяем, не оставлен ли уже отзыв
                var existingReview = await _dbManager.Reviews
                    .FirstOrDefaultAsync(r => r.BookingID == reviewData.BookingID);

                if (existingReview != null)
                {
                    return Conflict(new { message = "Отзыв на это бронирование уже оставлен" });
                }

                // Валидация рейтинга
                if (reviewData.Rating < 1 || reviewData.Rating > 5)
                {
                    return BadRequest(new { message = "Рейтинг должен быть от 1 до 5" });
                }

                // Создаем отзыв
                var review = new Review
                {
                    BookingID = reviewData.BookingID,
                    Rating = reviewData.Rating,
                    Comment = reviewData.Comment,
                    IsApproved = false, // Отзыв требует модерации
                    CreatedAt = DateTime.UtcNow
                };

                _dbManager.Reviews.Add(review);
                var createLog = new AuditLog(userId, "CREATE_REVIEW", "Reviews", review.ReviewID, null, $"Создан отзыв: BookingID={reviewData.BookingID}, Rating={reviewData.Rating}, Comment={reviewData.Comment?.Length ?? 0} символов",
   HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(createLog);
                await _dbManager.SaveChangesAsync();
                // Обновляем средний рейтинг друга
                await UpdateFriendAverageRating(booking.FriendProfileID);

                // Создаем уведомление для друга
                if (_dbManager.Notifications != null && booking.FriendProfile != null)
                {
                    var notification = new Notification
                    {
                        UserID = booking.FriendProfile.UserID,
                        Title = "Новый отзыв",
                        Message = $"Клиент оставил отзыв на вашу встречу. Рейтинг: {reviewData.Rating}/5",
                        Type = "Review",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbManager.Notifications.Add(notification);
                    await _dbManager.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Отзыв успешно создан и отправлен на модерацию",
                    reviewId = review.ReviewID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить отзывы о друге
        /// </summary>
        [Route("getByFriend/{friendProfileId}")]
        [HttpGet]
        public async Task<ActionResult> GetReviewsByFriend(
            int friendProfileId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool onlyApproved = true)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                var query = _dbManager.Reviews
                    .Include(r => r.Booking)
                    .ThenInclude(b => b != null ? b.Client : null)
                    .Where(r => r.Booking != null && r.Booking.FriendProfileID == friendProfileId);

                if (onlyApproved)
                {
                    query = query.Where(r => r.IsApproved);
                }

                var totalCount = await query.CountAsync();
                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => ReviewDTO.Convert(r))
                    .ToListAsync();

                // Средний рейтинг
                var avgRating = onlyApproved
                    ? await query.AverageAsync(r => (decimal?)r.Rating)
                    : null;

                return Ok(new
                {
                    message = "Отзывы получены",
                    averageRating = avgRating,
                    reviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить свои отзывы (как клиент)
        /// </summary>
        [Route("myReviews")]
        [HttpGet]
        public async Task<ActionResult> GetMyReviews(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var query = _dbManager.Reviews
                    .Include(r => r.Booking)
                    .ThenInclude(b => b != null ? b.FriendProfile : null)
                    .ThenInclude(fp => fp != null ? fp.User : null)
                    .Where(r => r.Booking != null && r.Booking.ClientID == userId);

                var totalCount = await query.CountAsync();
                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.ReviewID,
                        r.Rating,
                        r.Comment,
                        r.IsApproved,
                        r.CreatedAt,
                        FriendName = r.Booking != null && r.Booking.FriendProfile != null && r.Booking.FriendProfile.User != null
                            ? r.Booking.FriendProfile.User.FullName
                            : "Unknown",
                        FriendProfileID = r.Booking != null ? r.Booking.FriendProfileID : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Ваши отзывы",
                    reviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить отзывы на модерации (только администратор)
        /// </summary>
        [Route("pending")]
        [HttpGet]
        public async Task<ActionResult> GetPendingReviews(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем права администратора
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                {
                    return Forbid("Доступ запрещен. Требуются права администратора.");
                }

                var query = _dbManager.Reviews
                    .Include(r => r.Booking)
                    .ThenInclude(b => b != null ? b.Client : null)
                    .Where(r => !r.IsApproved);

                var totalCount = await query.CountAsync();
                var reviews = await query
                    .OrderBy(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.ReviewID,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        ClientName = r.Booking != null && r.Booking.Client != null
                            ? r.Booking.Client.FullName
                            : "Unknown",
                        r.BookingID,
                        FriendProfileID = r.Booking != null ? r.Booking.FriendProfileID : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Отзывы на модерации",
                    reviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Одобрить отзыв (только администратор)
        /// </summary>
        [Route("approve/{reviewId}")]
        [HttpPut]
        public async Task<ActionResult> ApproveReview(
            [FromHeader(Name = "TOKEN")] string token,
            int reviewId,
            [FromBody] ApproveReviewDTO approveData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем права администратора
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                {
                    return Forbid("Доступ запрещен. Требуются права администратора.");
                }

                var review = await _dbManager.Reviews
                    .Include(r => r.Booking)
                    .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

                if (review == null)
                {
                    return NotFound(new { message = "Отзыв не найден" });
                }

                if (review.IsApproved)
                {
                    return BadRequest(new { message = "Отзыв уже одобрен" });
                }
                var approveLog = new AuditLog(userId, "APPROVE_REVIEW", "Reviews", reviewId, $"IsApproved={review.IsApproved}", "IsApproved=true",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(approveLog);
                review.IsApproved = true;
                review.ModeratorComment = approveData.ModeratorComment;
                await _dbManager.SaveChangesAsync();

                // Обновляем средний рейтинг друга
                if (review.Booking != null)
                {
                    await UpdateFriendAverageRating(review.Booking.FriendProfileID);
                }

                // Создаем уведомление для друга
                if (_dbManager.Notifications != null && review.Booking?.FriendProfile != null)
                {
                    var notification = new Notification
                    {
                        UserID = review.Booking.FriendProfile.UserID,
                        Title = "Отзыв одобрен",
                        Message = $"Ваш отзыв от клиента был одобрен модерацией. Рейтинг: {review.Rating}/5",
                        Type = "Review",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbManager.Notifications.Add(notification);
                    await _dbManager.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Отзыв успешно одобрен",
                    reviewId = review.ReviewID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Отклонить отзыв (только администратор)
        /// </summary>
        [Route("reject/{reviewId}")]
        [HttpPut]
        public async Task<ActionResult> RejectReview(
            [FromHeader(Name = "TOKEN")] string token,
            int reviewId,
            [FromBody] RejectReviewDTO rejectData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем права администратора
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                {
                    return Forbid("Доступ запрещен. Требуются права администратора.");
                }

                var review = await _dbManager.Reviews
                    .FindAsync(reviewId);

                if (review == null)
                {
                    return NotFound(new { message = "Отзыв не найден" });
                }

                if (review.IsApproved)
                {
                    return BadRequest(new { message = "Нельзя отклонить уже одобренный отзыв" });
                }

                review.ModeratorComment = rejectData.ModeratorComment;
                var rejectLog = new AuditLog(userId, "REJECT_REVIEW", "Reviews", reviewId, $"Review content: Rating={review.Rating}, Comment={review.Comment}", $"Отклонен по причине: {rejectData.ModeratorComment}",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(rejectLog);
                _dbManager.Reviews.Remove(review);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Отзыв отклонен и удален",
                    reviewId = review.ReviewID,
                    reason = rejectData.ModeratorComment
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Удалить отзыв (администратор или автор)
        /// </summary>
        [Route("delete/{reviewId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteReview(
            [FromHeader(Name = "TOKEN")] string token,
            int reviewId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Reviews == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var review = await _dbManager.Reviews
                    .Include(r => r.Booking)
                    .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

                if (review == null)
                {
                    return NotFound(new { message = "Отзыв не найден" });
                }

                var currentUser = await _dbManager.Users.FindAsync(userId);
                var isAdmin = currentUser?.Role == "Admin";
                var isAuthor = review.Booking != null && review.Booking.ClientID == userId;

                if (!isAdmin && !isAuthor)
                {
                    return Forbid("Вы можете удалять только свои отзывы");
                }

                var friendProfileID = review.Booking?.FriendProfileID;
                var deleteLog = new AuditLog(userId, "DELETE_REVIEW", "Reviews", reviewId, $"Rating={review.Rating}, Comment={review.Comment}, IsApproved={review.IsApproved}", "Отзыв удален",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(deleteLog);
                _dbManager.Reviews.Remove(review);
                await _dbManager.SaveChangesAsync();

                if (friendProfileID.HasValue && review.IsApproved)
                {
                    await UpdateFriendAverageRating(friendProfileID.Value);
                }

                return Ok(new
                {
                    message = "Отзыв успешно удален",
                    reviewId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Вспомогательный метод: обновление среднего рейтинга друга
        /// </summary>
        private async Task UpdateFriendAverageRating(int friendProfileId)
        {
            if (_dbManager == null || _dbManager.Reviews == null || _dbManager.FriendProfiles == null)
                return;

            var averageRating = await _dbManager.Reviews
                .Where(r => r.Booking != null
                            && r.Booking.FriendProfileID == friendProfileId
                            && r.IsApproved)
                .AverageAsync(r => (decimal?)r.Rating);

            var friendProfile = await _dbManager.FriendProfiles
                .FindAsync(friendProfileId);

            if (friendProfile != null)
            {
                friendProfile.AverageRating = averageRating ?? 0;
                friendProfile.UpdatedAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();
            }
        }
    }
}