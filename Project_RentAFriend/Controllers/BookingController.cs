using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.BookingDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/booking")]
    public class BookingController : Controller
    {
        private readonly DBManager? _dbManager;

        public BookingController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Создать новое бронирование
        /// </summary>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> CreateBooking(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CreateBookingDTO bookingData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.Schedules == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Проверяем существование слота
                var schedule = await _dbManager.Schedules
                    .Include(s => s.FriendProfile)
                    .FirstOrDefaultAsync(s => s.ScheduleID == bookingData.ScheduleID);

                if (schedule == null)
                {
                    return NotFound(new { message = "Слот не найден" });
                }

                // Проверяем доступность слота
                if (!schedule.IsAvailable || schedule.BookingID != null)
                {
                    return BadRequest(new { message = "Слот уже забронирован" });
                }

                // Проверяем, что дата не в прошлом
                if (schedule.Date.Date < DateTime.UtcNow.Date)
                {
                    return BadRequest(new { message = "Нельзя забронировать прошедшую дату" });
                }

                // Проверяем профиль друга
                if (schedule.FriendProfile == null)
                {
                    return BadRequest(new { message = "Профиль друга не найден" });
                }

                // Рассчитываем стоимость
                var durationHours = (decimal)(schedule.EndTime - schedule.StartTime).TotalHours;
                var totalAmount = (schedule.FriendProfile.HourlyRate ?? 0) * durationHours;

                // Создаем бронирование
                var booking = new Booking
                {
                    ClientID = userId.Value,
                    FriendProfileID = schedule.ProfileID,
                    ScheduleID = bookingData.ScheduleID,
                    Purpose = bookingData.Purpose,
                    TotalAmount = totalAmount,
                    PaymentStatus = "Unpaid",
                    MeetingLocation = bookingData.MeetingLocation,
                    SpecialRequests = bookingData.SpecialRequests,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbManager.Bookings.Add(booking);
                await _dbManager.SaveChangesAsync();

                // Занять слот
                schedule.IsAvailable = false;
                schedule.BookingID = booking.BookingID;
                await _dbManager.SaveChangesAsync();

                // Создаем уведомление для друга
                if (_dbManager.Notifications != null)
                {
                    var notification = new Notification
                    {
                        UserID = schedule.FriendProfile.UserID,
                        Title = "Новое бронирование",
                        Message = $"У вас новое бронирование на {schedule.Date:yyyy-MM-dd} с {schedule.StartTime} до {schedule.EndTime}",
                        Type = "Booking",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbManager.Notifications.Add(notification);
                    await _dbManager.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Бронирование успешно создано",
                    bookingId = booking.BookingID,
                    totalAmount,
                    schedule = new
                    {
                        schedule.Date,
                        schedule.StartTime,
                        schedule.EndTime
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить мои бронирования (как клиент)
        /// </summary>
        [Route("myBookings")]
        [HttpGet]
        public async Task<ActionResult> GetMyBookings(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var query = _dbManager.Bookings
                    .Include(b => b.FriendProfile)
                    .ThenInclude(fp => fp != null ? fp.User : null)
                    .Include(b => b.Schedule)
                    .Where(b => b.ClientID == userId);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(b => b.Status == status);
                }

                var totalCount = await query.CountAsync();
                var bookings = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => ClientBookingDTO.Convert(b))
                    .ToListAsync();

                return Ok(new
                {
                    message = "Ваши бронирования",
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    bookings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить детали бронирования
        /// </summary>
        [Route("{bookingId}")]
        [HttpGet]
        public async Task<ActionResult> GetBookingDetails(
            [FromHeader(Name = "TOKEN")] string token,
            int bookingId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var booking = await _dbManager.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.FriendProfile)
                    .ThenInclude(fp => fp != null ? fp.User : null)
                    .Include(b => b.Schedule)
                    .Include(b => b.Review)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Бронирование не найдено" });
                }

                // Проверяем права (клиент или друг)
                var isClient = booking.ClientID == userId;
                var isFriend = booking.FriendProfile != null && booking.FriendProfile.UserID == userId;

                if (!isClient && !isFriend)
                {
                    return Forbid("Доступ запрещен");
                }

                var bookingDetails = BookingDetailsDTO.Convert(booking);

                return Ok(new
                {
                    message = "Детали бронирования",
                    booking = bookingDetails
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Отменить бронирование (клиент)
        /// </summary>
        [Route("cancel/{bookingId}")]
        [HttpPut]
        public async Task<ActionResult> CancelBooking(
            [FromHeader(Name = "TOKEN")] string token,
            int bookingId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.Schedules == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var booking = await _dbManager.Bookings
                    .Include(b => b.Schedule)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Бронирование не найдено" });
                }

                // Проверяем, что клиент отменяет
                if (booking.ClientID != userId)
                {
                    return Forbid("Только клиент может отменить бронирование");
                }

                // Проверяем статус
                if (booking.Status != "Pending" && booking.Status != "Confirmed")
                {
                    return BadRequest(new { message = $"Невозможно отменить бронирование со статусом {booking.Status}" });
                }

                // Проверяем, что дата не в прошлом
                if (booking.Schedule != null && booking.Schedule.Date.Date < DateTime.UtcNow.Date)
                {
                    return BadRequest(new { message = "Нельзя отменить прошедшую встречу" });
                }

                booking.Status = "Cancelled";
                booking.UpdatedAt = DateTime.UtcNow;

                // Освобождаем слот
                if (booking.Schedule != null)
                {
                    booking.Schedule.IsAvailable = true;
                    booking.Schedule.BookingID = null;
                }

                await _dbManager.SaveChangesAsync();

                // Создаем уведомление для друга
                if (_dbManager.FriendProfiles != null && _dbManager.Notifications != null)
                {
                    var friendProfile = await _dbManager.FriendProfiles
                        .FirstOrDefaultAsync(fp => fp.ProfileID == booking.FriendProfileID);

                    if (friendProfile != null)
                    {
                        var notification = new Notification
                        {
                            UserID = friendProfile.UserID,
                            Title = "Бронирование отменено",
                            Message = $"Клиент отменил бронирование",
                            Type = "Booking",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbManager.Notifications.Add(notification);
                        await _dbManager.SaveChangesAsync();
                    }
                }

                return Ok(new
                {
                    message = "Бронирование успешно отменено",
                    bookingId = booking.BookingID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Оплатить бронирование (клиент)
        /// </summary>
        [Route("pay/{bookingId}")]
        [HttpPut]
        public async Task<ActionResult> PayBooking(
            [FromHeader(Name = "TOKEN")] string token,
            int bookingId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var booking = await _dbManager.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Бронирование не найдено" });
                }

                // Проверяем, что клиент оплачивает
                if (booking.ClientID != userId)
                {
                    return Forbid("Только клиент может оплатить бронирование");
                }

                // Проверяем статус оплаты
                if (booking.PaymentStatus == "Paid")
                {
                    return BadRequest(new { message = "Бронирование уже оплачено" });
                }

                booking.PaymentStatus = "Paid";
                booking.PaymentDate = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Бронирование успешно оплачено",
                    bookingId = booking.BookingID,
                    amount = booking.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить ближайшие бронирования (клиент)
        /// </summary>
        [Route("upcoming")]
        [HttpGet]
        public async Task<ActionResult> GetUpcomingBookings(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int top = 5)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var bookings = await _dbManager.Bookings
                    .Include(b => b.FriendProfile)
                    .ThenInclude(fp => fp != null ? fp.User : null)
                    .Include(b => b.Schedule)
                    .Where(b => b.ClientID == userId
                                && (b.Status == "Pending" || b.Status == "Confirmed")
                                && b.Schedule != null
                                && b.Schedule.Date >= DateTime.UtcNow.Date)
                    .OrderBy(b => b.Schedule.Date)
                    .ThenBy(b => b.Schedule.StartTime)
                    .Take(top)
                    .Select(b => new
                    {
                        b.BookingID,
                        b.Status,
                        b.Purpose,
                        b.TotalAmount,
                        b.PaymentStatus,
                        FriendName = b.FriendProfile != null && b.FriendProfile.User != null
                            ? b.FriendProfile.User.FullName
                            : "Unknown",
                        Date = b.Schedule != null ? b.Schedule.Date : DateTime.MinValue,
                        StartTime = b.Schedule != null ? b.Schedule.StartTime : TimeSpan.Zero,
                        EndTime = b.Schedule != null ? b.Schedule.EndTime : TimeSpan.Zero,
                        b.MeetingLocation
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Ближайшие встречи",
                    count = bookings.Count,
                    bookings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить историю бронирований (клиент)
        /// </summary>
        [Route("history")]
        [HttpGet]
        public async Task<ActionResult> GetBookingHistory(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var query = _dbManager.Bookings
                    .Include(b => b.FriendProfile)
                    .ThenInclude(fp => fp != null ? fp.User : null)
                    .Include(b => b.Schedule)
                    .Where(b => b.ClientID == userId && b.Status == "Completed");

                var totalCount = await query.CountAsync();
                var bookings = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.BookingID,
                        b.Status,
                        b.Purpose,
                        b.TotalAmount,
                        b.PaymentStatus,
                        b.MeetingLocation,
                        FriendName = b.FriendProfile != null && b.FriendProfile.User != null
                            ? b.FriendProfile.User.FullName
                            : "Unknown",
                        Date = b.Schedule != null ? b.Schedule.Date : DateTime.MinValue,
                        b.CreatedAt,
                        HasReview = b.Review != null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "История встреч",
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    bookings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить статистику по бронированиям (клиент)
        /// </summary>
        [Route("statistics")]
        [HttpGet]
        public async Task<ActionResult> GetBookingStatistics([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var totalBookings = await _dbManager.Bookings.CountAsync(b => b.ClientID == userId);
                var activeBookings = await _dbManager.Bookings.CountAsync(b => b.ClientID == userId && (b.Status == "Pending" || b.Status == "Confirmed"));
                var completedBookings = await _dbManager.Bookings.CountAsync(b => b.ClientID == userId && b.Status == "Completed");
                var cancelledBookings = await _dbManager.Bookings.CountAsync(b => b.ClientID == userId && b.Status == "Cancelled");
                var totalSpent = await _dbManager.Bookings
                    .Where(b => b.ClientID == userId && b.PaymentStatus == "Paid")
                    .SumAsync(b => b.TotalAmount);
                var averageCheck = totalBookings > 0 ? totalSpent / totalBookings : 0;

                return Ok(new
                {
                    message = "Статистика бронирований",
                    statistics = new
                    {
                        totalBookings,
                        activeBookings,
                        completedBookings,
                        cancelledBookings,
                        totalSpent,
                        averageCheck
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}