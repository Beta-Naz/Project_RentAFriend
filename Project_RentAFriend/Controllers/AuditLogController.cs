using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.AuditLogDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/audit")]
    public class AuditLogController : Controller
    {
        private readonly DBManager? _dbManager;

        public AuditLogController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Получить все логи (только для администратора)
        /// </summary>
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAllLogs(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (_dbManager == null || _dbManager.AuditLogs == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен и права администратора
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

                // Получаем логи с пагинацией
                var query = _dbManager.AuditLogs
                    .Include(al => al.User)
                    .OrderByDescending(al => al.LoggedAt);

                var totalCount = await query.CountAsync();
                var logs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(al => AuditLogDTO.Convert(al))
                    .ToListAsync();

                return Ok(new
                {
                    message = "Логи успешно получены",
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        /// <summary>
        /// Получить статистику по логам (только для администратора)
        /// </summary>
        [Route("statistics")]
        [HttpGet]
        public async Task<ActionResult> GetLogStatistics([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.AuditLogs == null || _dbManager.Users == null)
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

                // Общая статистика
                var totalLogs = await _dbManager.AuditLogs.CountAsync();

                // Статистика по действиям
                var actionsStats = await _dbManager.AuditLogs
                    .GroupBy(al => al.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // Статистика по дням (последние 30 дней)
                var last30Days = DateTime.UtcNow.AddDays(-30);
                var dailyStats = await _dbManager.AuditLogs
                    .Where(al => al.LoggedAt >= last30Days)
                    .GroupBy(al => al.LoggedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

                // Статистика по таблицам
                var tableStats = await _dbManager.AuditLogs
                    .GroupBy(al => al.TableName)
                    .Select(g => new { Table = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Статистика логов",
                    totalLogs,
                    topActions = actionsStats,
                    dailyStats,
                    topTables = tableStats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Создать свой лог (для любого пользователя)
        /// </summary>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> CreateLog(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CreateLogDTO logData)
        {
            try
            {
                if (_dbManager == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Создаем лог
                var newLog = new AuditLog
                {
                    UserID = userId,
                    Action = logData.Action,
                    TableName = logData.TableName,
                    RecordID = logData.RecordID,
                    OldValue = logData.OldValue,
                    NewValue = logData.NewValue,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    LoggedAt = DateTime.UtcNow
                };

                _dbManager.AuditLogs.Add(newLog);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Лог успешно создан",
                    logId = newLog.LogID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        /// <summary>
        /// Удалить все логи (только для администратора)
        /// </summary>
        [Route("deleteAll")]
        [HttpDelete]
        public async Task<ActionResult> DeleteAllLogs([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.AuditLogs == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен и права администратора
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

                // Получаем количество логов перед удалением
                var totalCount = await _dbManager.AuditLogs.CountAsync();

                if (totalCount == 0)
                {
                    return Ok(new { message = "Нет логов для удаления" });
                }

                // Удаляем все логи
                _dbManager.AuditLogs.RemoveRange(_dbManager.AuditLogs);
                await _dbManager.SaveChangesAsync();

                // Создаем лог о массовом удалении
                var deleteLog = new AuditLog
                {
                    UserID = userId,
                    Action = "DELETE_ALL_LOGS",
                    TableName = "AuditLogs",
                    RecordID = 0,
                    OldValue = $"Было удалено {totalCount} записей",
                    NewValue = "Все логи удалены",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    LoggedAt = DateTime.UtcNow
                };

                _dbManager.AuditLogs.Add(deleteLog);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Успешно удалено {totalCount} логов",
                    deletedCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}