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
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.Schedules == null || _dbManager.AuditLogs == null)
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
                var createLog = new AuditLog(userId, "CREATE_BOOKING", "Bookings", booking.BookingID, null, $"Создано бронирование: ProfileID={bookingData.ScheduleID}, Purpose={bookingData.Purpose}, Amount={totalAmount}",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(createLog);

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
                    .Include(b => b.Review)
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
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.Schedules == null || _dbManager.AuditLogs == null)
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

                var cancelLog = new AuditLog(userId, "CANCEL_BOOKING", "Bookings", bookingId, $"Status={booking.Status}", "Status=Cancelled",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(cancelLog);
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
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.AuditLogs == null)
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
                var payLog = new AuditLog(userId, "PAY_BOOKING", "Bookings", bookingId, $"PaymentStatus={booking.PaymentStatus}", "PaymentStatus=Paid",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(payLog);
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
                    .Where(b => b.ClientID == userId)
                    .Where(b => b.Status == "Pending" || b.Status == "Confirmed")
                    .Where(b => b.Schedule != null && b.Schedule.Date >= DateTime.UtcNow.Date)
                    .OrderBy(b => b.Schedule!.Date)
                    .ThenBy(b => b.Schedule!.StartTime)
                    .Take(top)
                    .Select(b => new
                    {
                        b.BookingID,
                        b.Status,
                        b.Purpose,
                        b.TotalAmount,
                        b.PaymentStatus,
                        FriendName = b.FriendProfile!.User!.FullName ?? "Unknown",
                        b.Schedule!.Date,
                        b.Schedule!.StartTime,
                        b.Schedule!.EndTime,
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
        /// <summary>
        /// Обновить статус бронирования
        /// </summary>
        [Route("updateStatus/{bookingId}")]
        [HttpPut]
        public async Task<ActionResult> UpdateBookingStatus(
            [FromHeader(Name = "TOKEN")] string token,
            int bookingId,
            [FromForm] string newStatus)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                if (newStatus == "Rejected" || newStatus == "Cancelled")
                {
                    return BadRequest(new { message = $"К сожалению вы не можете использовать этот запрос для отклонения или отмены бронирования" });
                }
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }
                var booking = await _dbManager.Bookings
                    .Include(b => b.FriendProfile)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Бронирование не найдено" });
                }

                if (booking.FriendProfile == null || booking.FriendProfile.UserID != userId)
                {
                    return Forbid("Только друг может изменить статус бронирования");
                }

                // Проверка допустимости перехода
                if (!IsValidStatusTransition(booking.Status, newStatus))
                {
                    return BadRequest(new { message = $"Невозможно изменить статус с {booking.Status} на {newStatus}" });
                }
                                // Если бронирование отклонено или отменено - освобождаем слот
                if (newStatus == "Rejected" || newStatus == "Cancelled")
                {
                    return BadRequest(new { message = $"К сожалению вы не можете использовать этот запрос для отклонения бронирования" });
                }
                booking.Status = newStatus;
                booking.UpdatedAt = DateTime.UtcNow;
                var updateStatusLog = new AuditLog(userId, $"UPDATE_BOOKING_STATUS_TO_{newStatus.ToUpper()}", "Bookings", bookingId, $"Status={booking.Status}", $"Status={newStatus}",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(updateStatusLog);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Статус бронирования изменен на {newStatus}",
                    BookingId = booking.BookingID,
                    newStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Отклонить бронирование
        /// </summary>
        [Route("reject/{bookingId}")]
        [HttpPut]
        public async Task<ActionResult> RejectBooking(
            [FromHeader(Name = "TOKEN")] string token,
            int bookingId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.Users == null || _dbManager.AuditLogs == null || _dbManager.Schedules == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var booking = await _dbManager.Bookings
                    .Include(b => b.FriendProfile)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Бронирование не найдено" });
                }

                if (booking.FriendProfile == null || booking.FriendProfile.UserID != userId)
                {
                    return Forbid("Только друг может отклонить бронирование");
                }

                if (booking.Status != "Pending")
                {
                    return BadRequest(new { message = $"Невозможно отклонить бронирование со статусом {booking.Status}" });
                }

                booking.Status = "Rejected";
                booking.UpdatedAt = DateTime.UtcNow;

                var schedule = await _dbManager.Schedules.FindAsync(booking.ScheduleID);
                if (schedule != null)
                {
                    schedule.IsAvailable = true;
                    schedule.BookingID = null;
                }
                var rejectLog = new AuditLog(userId, "REJECT_BOOKING", "Bookings", bookingId, $"Status={booking.Status}", "Status=Rejected",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(rejectLog);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Бронирование отклонено",
                    BookingId = booking.BookingID,
                    Reason = "Бронирование отклонено другом"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить бронирования друга
        /// </summary>
        [Route("friendBookings/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetFriendBookings(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId,
            [FromQuery] string? status = null)
        {
            try
            {
                if (_dbManager == null || _dbManager.Bookings == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId && fp.UserID == userId);

                if (friendProfile == null)
                {
                    return Forbid("Доступ запрещен");
                }

                var query = _dbManager.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Schedule)
                    .Where(b => b.FriendProfileID == profileId);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(b => b.Status == status);
                }

                var bookings = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookingDetailsDTO
                    {
                        BookingID = b.BookingID,
                        ClientID = b.ClientID,
                        ClientName = b.Client != null ? b.Client.FullName : "Unknown",
                        Status = b.Status,
                        TotalAmount = b.TotalAmount,
                        PaymentStatus = b.PaymentStatus,
                        Purpose = b.Purpose,
                        StartTime = b.Schedule != null ? b.Schedule.StartTime : TimeSpan.Zero,
                        EndTime = b.Schedule != null ? b.Schedule.EndTime : TimeSpan.Zero,
                        MeetingLocation = b.MeetingLocation,
                        SpecialRequests = b.SpecialRequests,
                        CreatedAt = b.CreatedAt,
                        HasReview = b.Review != null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Бронирования получены",
                    Bookings = bookings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        /// <summary>
        /// Проверяет допустимость перехода статуса бронирования
        /// </summary>
        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            // Допустимые переходы статусов
            return (currentStatus, newStatus) switch
            {
                // Из Pending можно в Confirmed, Rejected
                ("Pending", "Confirmed") => true,
                ("Pending", "Rejected") => true,

                // Из Confirmed можно в Completed, Cancelled
                ("Confirmed", "Completed") => true,
                ("Confirmed", "Cancelled") => true,

                // Завершенные и отмененные статусы - конечные, их нельзя менять
                ("Completed", _) => false,
                ("Cancelled", _) => false,
                ("Rejected", _) => false,

                // Любые другие переходы запрещены
                _ => false
            };
        }
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAllBookings(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (_dbManager?.Bookings == null || _dbManager.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Forbid("Доступ запрещен");

                var bookings = await _dbManager.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Schedule)
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.BookingID,
                        b.ClientID,
                        ClientName = b.Client.FullName,
                        b.Status,
                        b.TotalAmount,
                        b.PaymentStatus,
                        b.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { message = "Бронирования получены", bookings });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}