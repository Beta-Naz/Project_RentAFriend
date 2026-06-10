using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.FriendProfileDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/friend")]
    public class FriendProfileController : Controller
    {
        private readonly DBManager? _dbManager;

        public FriendProfileController()
        {
            _dbManager = new DBManager();
        }
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> Create([FromHeader(Name = "TOKEN")] string token, [FromBody] FPMainInfoDTO infoDTO)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                int? userId = JwtToken.GetUserIdFromToken(token);
                if(userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }
                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }
                if (user.Role != "Friend")
                {
                    return Conflict(new { message = "только друг может создать профиль" });
                }
                if (infoDTO == null)
                {
                    return Conflict(new { message = "Нету данных для профиля" });
                }
                FriendProfile newFriendProfile = new()
                {
                    Bio = infoDTO.Bio,
                    Hobbies = infoDTO.Hobbies,
                    HourlyRate = infoDTO.HourlyRate,
                    City = infoDTO.City,
                    Age = infoDTO.Age,
                    IsVerified = false,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UserID = user.UserID
                };
                _dbManager.FriendProfiles.Add(newFriendProfile);
                await _dbManager.SaveChangesAsync();
                FPInfoDTO dataInfo = FPInfoDTO.Convert(newFriendProfile);
                return Ok(new 
                { 
                    message = "Профиль успешно создан",
                    dataInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("update")]
        [HttpPut]
        public async Task<ActionResult> Update([FromHeader(Name = "TOKEN")] string token, [FromBody] FPMainInfoDTO infoDTO)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                if (user.Role != "Friend")
                {
                    return BadRequest(new { message = "Только друзья могут обновлять профиль" });
                }

                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return NotFound(new { message = "Профиль друга не найден. Сначала создайте профиль." });
                }

                if (infoDTO == null)
                {
                    return BadRequest(new { message = "Нет данных для обновления профиля" });
                }

                if (infoDTO.Bio != null)
                    friendProfile.Bio = infoDTO.Bio;

                if (infoDTO.Hobbies != null)
                    friendProfile.Hobbies = infoDTO.Hobbies;

                if (infoDTO.HourlyRate.HasValue)
                {
                    if (infoDTO.HourlyRate <= 0)
                    {
                        return BadRequest(new { message = "Почасовая ставка должна быть больше 0" });
                    }
                    friendProfile.HourlyRate = infoDTO.HourlyRate;
                }

                if (infoDTO.City != null)
                    friendProfile.City = infoDTO.City;

                if (infoDTO.Age.HasValue)
                {
                    if (infoDTO.Age < 18 || infoDTO.Age > 99)
                    {
                        return BadRequest(new { message = "Возраст должен быть от 18 до 99 лет" });
                    }
                    friendProfile.Age = infoDTO.Age;
                }

                friendProfile.UpdatedAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Профиль успешно обновлен"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAll([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var profiles = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .Where(fp => fp.User != null && fp.User.IsActive && fp.User.Role == "Friend")
                    .OrderByDescending(fp => fp.AverageRating)
                    .ThenByDescending(fp => fp.CreatedAt)
                    .ToListAsync();

                if (profiles == null || profiles.Count == 0)
                {
                    return Ok(new { message = "Профили не найдены", profiles = new List<FPInfoDTO>() });
                }

                var profilesDTO = profiles.Select(p => FPInfoDTO.Convert(p)).ToList();

                return Ok(new
                {
                    message = "Профили успешно получены",
                    count = profilesDTO.Count,
                    profiles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("myProfile")]
        [HttpGet]
        public async Task<ActionResult> GetMyProfile([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                if (user.Role != "Friend")
                {
                    return BadRequest(new { message = "Только друзья имеют профиль" });
                }

                var friendProfile = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                {
                    return NotFound(new { message = "Профиль не найден. Создайте профиль сначала." });
                }

                var profileDTO = FPInfoDTO.Convert(friendProfile);

                return Ok(new
                {
                    message = "Профиль успешно получен",
                    profile = profileDTO
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        /// <summary>
        /// Получить профиль друга по ID
        /// </summary>
        [Route("profile/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetFriendProfileById(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId)
        {
            try
            {
                if (_dbManager == null || _dbManager.FriendProfiles == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Получаем профиль
                var friendProfile = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId);

                if (friendProfile == null)
                {
                    return NotFound(new { message = "Профиль не найден" });
                }

                // Проверяем, что пользователь активен
                if (friendProfile.User == null || !friendProfile.User.IsActive)
                {
                    return NotFound(new { message = "Профиль недоступен" });
                }

                var profileDTO = FPInfoDTO.Convert(friendProfile);

                return Ok(new
                {
                    message = "Профиль успешно получен",
                    profile = profileDTO
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        /// <summary>
        /// Получить статистику профиля друга
        /// </summary>
        [Route("stats/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetFriendProfileStats(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId)
        {
            try
            {
                if (_dbManager == null || _dbManager.FriendProfiles == null || _dbManager.Bookings == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                // Проверяем токен
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Проверяем существование профиля
                var profile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId);

                if (profile == null)
                {
                    return NotFound(new { message = "Профиль не найден" });
                }

                // Статистика бронирований
                var totalBookings = await _dbManager.Bookings
                    .CountAsync(b => b.FriendProfileID == profileId);

                var completedBookings = await _dbManager.Bookings
                    .CountAsync(b => b.FriendProfileID == profileId && b.Status == "Completed");

                var totalEarnings = await _dbManager.Bookings
                    .Where(b => b.FriendProfileID == profileId && b.Status == "Completed" && b.PaymentStatus == "Paid")
                    .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

                var averageRating = await _dbManager.Reviews
                    .Where(r => r.Booking != null && r.Booking.FriendProfileID == profileId && r.IsApproved)
                    .AverageAsync(r => (decimal?)r.Rating) ?? 0;

                var reviewCount = await _dbManager.Reviews
                    .CountAsync(r => r.Booking != null && r.Booking.FriendProfileID == profileId && r.IsApproved);

                var stats = new FPStatsDTO
                {
                    TotalBookings = totalBookings,
                    CompletedBookings = completedBookings,
                    TotalEarnings = totalEarnings,
                    AverageRating = (decimal)averageRating,
                    ReviewCount = reviewCount
                };

                return Ok(new
                {
                    message = "Статистика получена",
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить ближайшие встречи друга
        /// </summary>
        [Route("upcomingMeetings/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetUpcomingMeetings(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId,
            [FromQuery] int top = 5)
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

                // Проверяем существование профиля
                var profile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId);

                if (profile == null)
                {
                    return NotFound(new { message = "Профиль не найден" });
                }

                // Получаем ближайшие встречи
                var meetings = await _dbManager.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Schedule)
                    .Where(b => b.FriendProfileID == profileId
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
                        b.MeetingLocation,
                        ClientName = b.Client != null ? b.Client.FullName : "Unknown",
                        ScheduleDate = b.Schedule != null ? b.Schedule.Date : DateTime.MinValue,
                        StartTime = b.Schedule != null ? b.Schedule.StartTime : TimeSpan.Zero,
                        EndTime = b.Schedule != null ? b.Schedule.EndTime : TimeSpan.Zero
                    })
                    .ToListAsync();

                var meetingsList = meetings.Select(m => new UpcomingMeetingItem
                {
                    BookingID = m.BookingID,
                    ClientName = m.ClientName,
                    Status = m.Status,
                    Purpose = m.Purpose,
                    TotalAmount = m.TotalAmount,
                    PaymentStatus = m.PaymentStatus,
                    MeetingLocation = m.MeetingLocation,
                    ScheduleDate = m.ScheduleDate,
                    StartTime = m.StartTime,
                    EndTime = m.EndTime
                }).ToList();

                return Ok(new
                {
                    message = "Ближайшие встречи получены",
                    count = meetingsList.Count,
                    meetings = meetingsList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        /// <summary>
        /// Получить доступные города для фильтрации
        /// </summary>
        [Route("cities")]
        [HttpGet]
        public async Task<ActionResult> GetAvailableCities([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var cities = await _dbManager.FriendProfiles
                    .Where(fp => fp.City != null && fp.City != "" && fp.IsVerified)
                    .Select(fp => fp.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("verify/{profileId}")]
        [HttpPut]
        public async Task<ActionResult> VerifyFriendProfile(
    [FromHeader(Name = "TOKEN")] string token,
    int profileId,
    [FromBody] VerifyProfileDTO data)
        {
            try
            {
                if (_dbManager?.FriendProfiles == null || _dbManager.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Forbid("Доступ запрещен");

                var profile = await _dbManager.FriendProfiles.FindAsync(profileId);
                if (profile == null)
                    return NotFound(new { message = "Профиль не найден" });

                profile.IsVerified = data.IsVerified;
                await _dbManager.SaveChangesAsync();

                return Ok(new { result = true, message = data.IsVerified ? "Профиль верифицирован" : "Верификация отклонена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}
