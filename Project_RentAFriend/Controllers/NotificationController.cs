using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Context;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.NotificationDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/notification")]
    public class NotificationController : Controller
    {
        private readonly DBManager? _dbManager;

        public NotificationController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Получить все уведомления пользователя
        /// </summary>
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetMyNotifications(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool onlyUnread = false)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Базовый запрос
                var query = _dbManager.Notifications
                    .Where(n => n.UserID == userId)
                    .AsQueryable();

                // Фильтр по непрочитанным
                if (onlyUnread)
                {
                    query = query.Where(n => !n.IsRead);
                }

                // Пагинация и сортировка
                var totalCount = await query.CountAsync();
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => NotificationDTO.Convert(n))
                    .ToListAsync();

                // Счетчик непрочитанных
                var unreadCount = await _dbManager.Notifications
                    .CountAsync(n => n.UserID == userId && !n.IsRead);

                return Ok(new
                {
                    message = "Уведомления получены",
                    unreadCount,
                    notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить непрочитанные уведомления
        /// </summary>
        [Route("unread")]
        [HttpGet]
        public async Task<ActionResult> GetUnreadNotifications(
            [FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var notifications = await _dbManager.Notifications
                    .Where(n => n.UserID == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => NotificationDTO.Convert(n))
                    .ToListAsync();

                return Ok(new
                {
                    message = "Непрочитанные уведомления",
                    count = notifications.Count,
                    notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Отметить уведомление как прочитанное
        /// </summary>
        [Route("markAsRead/{notificationId}")]
        [HttpPut]
        public async Task<ActionResult> MarkAsRead(
            [FromHeader(Name = "TOKEN")] string token,
            int notificationId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var notification = await _dbManager.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

                if (notification == null)
                {
                    return NotFound(new { message = "Уведомление не найдено" });
                }

                if (notification.IsRead)
                {
                    return Ok(new { message = "Уведомление уже прочитано" });
                }

                notification.IsRead = true;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Уведомление отмечено как прочитанное",
                    notificationId = notification.NotificationID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Отметить все уведомления как прочитанные
        /// </summary>
        [Route("markAllAsRead")]
        [HttpPut]
        public async Task<ActionResult> MarkAllAsRead([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var unreadNotifications = await _dbManager.Notifications
                    .Where(n => n.UserID == userId && !n.IsRead)
                    .ToListAsync();

                var updatedCount = unreadNotifications.Count;

                if (updatedCount == 0)
                {
                    return Ok(new { message = "Нет непрочитанных уведомлений" });
                }

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Отмечено {updatedCount} уведомлений как прочитанные",
                    updatedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Создать уведомление (для администратора или системы)
        /// </summary>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> CreateNotification(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CreateNotificationDTO notificationData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен и права
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var currentUser = await _dbManager.Users.FindAsync(userId);
                var isAdmin = currentUser?.Role == "Admin";

                // Если не админ, то может создать только себе
                if (!isAdmin && notificationData.UserID != userId)
                {
                    return Forbid("Вы можете создавать уведомления только для себя");
                }

                // Проверяем существование пользователя
                var targetUser = await _dbManager.Users.FindAsync(notificationData.UserID);
                if (targetUser == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Валидация типа уведомления
                var allowedTypes = new[] { "Booking", "Message", "System", "Review", "Verification", "Info" };
                if (!allowedTypes.Contains(notificationData.Type))
                {
                    return BadRequest(new { message = "Недопустимый тип уведомления" });
                }

                // Создаем уведомление
                var notification = new Notification
                {
                    UserID = notificationData.UserID,
                    Title = notificationData.Title,
                    Message = notificationData.Message,
                    Type = notificationData.Type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbManager.Notifications.Add(notification);
                await _dbManager.SaveChangesAsync();

                // Логируем действие
                if (_dbManager.AuditLogs != null)
                {
                    _dbManager.AuditLogs.Add(new AuditLog
                    {
                        UserID = userId,
                        Action = "CREATE_NOTIFICATION",
                        TableName = "Notifications",
                        RecordID = notification.NotificationID,
                        NewValue = $"Created for UserID={notificationData.UserID}",
                        LoggedAt = DateTime.UtcNow
                    });
                    await _dbManager.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Уведомление успешно создано",
                    notificationId = notification.NotificationID,
                    notification = NotificationDTO.Convert(notification)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Удалить уведомление
        /// </summary>
        [Route("delete/{notificationId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteNotification(
            [FromHeader(Name = "TOKEN")] string token,
            int notificationId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var notification = await _dbManager.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationID == notificationId);

                if (notification == null)
                {
                    return NotFound(new { message = "Уведомление не найдено" });
                }

                var currentUser = await _dbManager.Users.FindAsync(userId);
                var isAdmin = currentUser?.Role == "Admin";

                if (notification.UserID != userId && !isAdmin)
                {
                    return Forbid("Вы можете удалять только свои уведомления");
                }

                _dbManager.Notifications.Remove(notification);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Уведомление удалено",
                    notificationId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Удалить все уведомления пользователя
        /// </summary>
        [Route("deleteAll")]
        [HttpDelete]
        public async Task<ActionResult> DeleteAllNotifications([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var notifications = await _dbManager.Notifications
                    .Where(n => n.UserID == userId)
                    .ToListAsync();

                var deletedCount = notifications.Count;

                if (deletedCount == 0)
                {
                    return Ok(new { message = "Нет уведомлений для удаления" });
                }

                _dbManager.Notifications.RemoveRange(notifications);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Удалено {deletedCount} уведомлений",
                    deletedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить количество непрочитанных уведомлений
        /// </summary>
        [Route("unreadCount")]
        [HttpGet]
        public async Task<ActionResult> GetUnreadCount([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Notifications == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var unreadCount = await _dbManager.Notifications
                    .CountAsync(n => n.UserID == userId && !n.IsRead);

                return Ok(new
                {
                    unreadCount,
                    hasUnread = unreadCount > 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}