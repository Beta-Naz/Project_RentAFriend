using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.ScheduleDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/schedule")]
    public class ScheduleController : Controller
    {
        private readonly DBManager? _dbManager;
        
        public ScheduleController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Получить расписание на конкретную дату
        /// </summary>
        [Route("getByDate/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetScheduleByDate(
            int profileId, 
            [FromQuery] DateTime date)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                var schedule = await _dbManager.Schedules
                    .Where(s => s.ProfileID == profileId && s.Date.Date == date.Date)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new ScheduleDTO
                    {
                        ScheduleID = s.ScheduleID,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        IsAvailable = s.IsAvailable && s.BookingID == null,
                        IsBooked = s.BookingID != null,
                        BookingID = s.BookingID
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Расписание получено",
                    date = date.Date,
                    slots = schedule
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить доступные слоты для бронирования
        /// </summary>
        [Route("available/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetAvailableTimeSlots(
            int profileId, 
            [FromQuery] DateTime date)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                var availableSlots = await _dbManager.Schedules
                    .Where(s => s.ProfileID == profileId 
                                && s.Date.Date == date.Date
                                && s.IsAvailable == true
                                && s.BookingID == null
                                && s.Date >= DateTime.UtcNow.Date)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new AvailableSlotDTO
                    {
                        ScheduleID = s.ScheduleID,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Доступные слоты",
                    date = date.Date,
                    count = availableSlots.Count,
                    slots = availableSlots
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Создать временной слот (для друга)
        /// </summary>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> CreateTimeSlot(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CreateScheduleDTO scheduleData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Проверяем, что пользователь - друг
                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return BadRequest(new { message = "Профиль друга не найден" });
                }

                // Валидация времени
                if (scheduleData.EndTime <= scheduleData.StartTime)
                {
                    return BadRequest(new { message = "Время окончания должно быть позже времени начала" });
                }

                var durationMinutes = (scheduleData.EndTime - scheduleData.StartTime).TotalMinutes;
                if (durationMinutes < 30)
                {
                    return BadRequest(new { message = "Минимальная продолжительность - 30 минут" });
                }
                if (durationMinutes > 480)
                {
                    return BadRequest(new { message = "Максимальная продолжительность - 8 часов" });
                }

                // Проверка на пересечение с существующими слотами
                var hasOverlap = await _dbManager.Schedules
                    .AnyAsync(s => s.ProfileID == friendProfile.ProfileID
                                   && s.Date.Date == scheduleData.Date.Date
                                   && s.StartTime < scheduleData.EndTime
                                   && s.EndTime > scheduleData.StartTime);

                if (hasOverlap)
                {
                    return Conflict(new { message = "Этот временной слот пересекается с существующим" });
                }

                // Создаем слот
                var newSlot = new Schedule
                {
                    ProfileID = friendProfile.ProfileID,
                    Date = scheduleData.Date.Date,
                    StartTime = scheduleData.StartTime,
                    EndTime = scheduleData.EndTime,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbManager.Schedules.Add(newSlot);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Слот успешно создан",
                    schedule = new
                    {
                        newSlot.ScheduleID,
                        newSlot.Date,
                        newSlot.StartTime,
                        newSlot.EndTime,
                        newSlot.IsAvailable
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Удалить временной слот (для друга)
        /// </summary>
        [Route("delete/{scheduleId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteTimeSlot(
            [FromHeader(Name = "TOKEN")] string token,
            int scheduleId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Получаем профиль друга
                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return BadRequest(new { message = "Профиль друга не найден" });
                }

                // Ищем слот
                var slot = await _dbManager.Schedules
                    .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId && s.ProfileID == friendProfile.ProfileID);

                if (slot == null)
                {
                    return NotFound(new { message = "Слот не найден" });
                }

                // Проверяем, не забронирован ли слот
                if (slot.BookingID != null)
                {
                    return BadRequest(new { message = "Невозможно удалить забронированный слот" });
                }

                _dbManager.Schedules.Remove(slot);
                await _dbManager.SaveChangesAsync();

                return Ok(new { message = "Слот успешно удален" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Обновить доступность слота
        /// </summary>
        [Route("updateAvailability/{scheduleId}")]
        [HttpPut]
        public async Task<ActionResult> UpdateTimeSlotAvailability(
            [FromHeader(Name = "TOKEN")] string token,
            int scheduleId,
            [FromBody] UpdateAvailabilityDTO availabilityData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Получаем профиль друга
                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return BadRequest(new { message = "Профиль друга не найден" });
                }

                // Ищем слот
                var slot = await _dbManager.Schedules
                    .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId && s.ProfileID == friendProfile.ProfileID);

                if (slot == null)
                {
                    return NotFound(new { message = "Слот не найден" });
                }

                // Проверяем, не забронирован ли слот
                if (slot.BookingID != null)
                {
                    return BadRequest(new { message = "Невозможно изменить доступность забронированного слота" });
                }

                slot.IsAvailable = availabilityData.IsAvailable;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Слот {(availabilityData.IsAvailable ? "доступен" : "недоступен")}",
                    scheduleId = slot.ScheduleID,
                    isAvailable = slot.IsAvailable
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Очистить расписание на дату
        /// </summary>
        [Route("clearDate")]
        [HttpDelete]
        public async Task<ActionResult> ClearScheduleForDate(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] DateTime date)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Получаем профиль друга
                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return BadRequest(new { message = "Профиль друга не найден" });
                }

                // Проверяем, есть ли забронированные слоты
                var hasBookedSlots = await _dbManager.Schedules
                    .AnyAsync(s => s.ProfileID == friendProfile.ProfileID 
                                   && s.Date.Date == date.Date 
                                   && s.BookingID != null);

                if (hasBookedSlots)
                {
                    return BadRequest(new { message = "Невозможно очистить дату с забронированными слотами" });
                }

                // Удаляем все слоты на дату
                var slotsToDelete = await _dbManager.Schedules
                    .Where(s => s.ProfileID == friendProfile.ProfileID && s.Date.Date == date.Date)
                    .ToListAsync();

                var deletedCount = slotsToDelete.Count;
                _dbManager.Schedules.RemoveRange(slotsToDelete);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Очищено {deletedCount} слотов на {date.Date:yyyy-MM-dd}",
                    deletedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Создать стандартное недельное расписание
        /// </summary>
        [Route("createWeekSchedule")]
        [HttpPost]
        public async Task<ActionResult> CreateDefaultWeekSchedule(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CreateWeekScheduleDTO weekData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Получаем профиль друга
                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return BadRequest(new { message = "Профиль друга не найден" });
                }

                var startDate = weekData.StartDate.Date;
                var slotsCreated = new List<Schedule>();

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = startDate.AddDays(i);

                    // Утренний слот: 9:00-13:00
                    slotsCreated.Add(new Schedule
                    {
                        ProfileID = friendProfile.ProfileID,
                        Date = currentDate,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(13, 0, 0),
                        IsAvailable = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Дневной слот: 14:00-18:00
                    slotsCreated.Add(new Schedule
                    {
                        ProfileID = friendProfile.ProfileID,
                        Date = currentDate,
                        StartTime = new TimeSpan(14, 0, 0),
                        EndTime = new TimeSpan(18, 0, 0),
                        IsAvailable = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _dbManager.Schedules.AddRange(slotsCreated);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Создано {slotsCreated.Count} слотов на неделю",
                    slotsCount = slotsCreated.Count,
                    startDate,
                    endDate = startDate.AddDays(6)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Проверить пересечение временных слотов
        /// </summary>
        [Route("checkOverlap")]
        [HttpPost]
        public async Task<ActionResult> CheckTimeSlotOverlap(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] CheckOverlapDTO overlapData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Schedules == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var query = _dbManager.Schedules
                    .Where(s => s.ProfileID == overlapData.ProfileID
                                && s.Date.Date == overlapData.Date.Date
                                && s.StartTime < overlapData.EndTime
                                && s.EndTime > overlapData.StartTime);

                if (overlapData.ScheduleID.HasValue)
                {
                    query = query.Where(s => s.ScheduleID != overlapData.ScheduleID.Value);
                }

                var hasOverlap = await query.AnyAsync();

                return Ok(new
                {
                    hasOverlap,
                    message = hasOverlap ? "Обнаружено пересечение с существующим слотом" : "Слоты не пересекаются"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}