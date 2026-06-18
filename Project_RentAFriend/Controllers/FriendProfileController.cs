using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Context;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.FriendProfileDTO;
using Project_RentAFriend.Models.ClassesDTO.NotificationDTO;

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
                if (_dbManager?.Users == null || _dbManager.FriendProfiles == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                    return NotFound(new { ok = false, message = "Пользователь не найден" });

                if (user.Role != "Friend")
                    return BadRequest(new { ok = false, message = "Только друг может создать профиль" });

                if (infoDTO == null)
                    return BadRequest(new { ok = false, message = "Нет данных для профиля" });

                bool profileExists = await _dbManager.FriendProfiles.AnyAsync(fp => fp.UserID == user.UserID);
                if (profileExists)
                    return Conflict(new { ok = false, message = "У вас уже есть профиль друга" });

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
                var createLog = new AuditLog(userId, "CREATE_FRIEND_PROFILE", "FriendProfiles", newFriendProfile.ProfileID, null, 
                    $"Создан профиль друга: City={infoDTO.City}, Age={infoDTO.Age}, HourlyRate={infoDTO.HourlyRate}",
                HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(createLog);
                await _dbManager.SaveChangesAsync();

                var createdProfile = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .FirstAsync(fp => fp.ProfileID == newFriendProfile.ProfileID);

                var dataInfo = FPInfoDTO.Convert(createdProfile);

                return Ok(new { ok = true, message = "Профиль успешно создан", profile = dataInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("update")]
        [HttpPut]
        public async Task<ActionResult> Update([FromHeader(Name = "TOKEN")] string token, [FromBody] FPMainInfoDTO infoDTO)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.FriendProfiles == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                    return NotFound(new { ok = false, message = "Пользователь не найден" });

                if (user.Role != "Friend")
                    return BadRequest(new { ok = false, message = "Только друзья могут обновлять профиль" });

                var friendProfile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                    return NotFound(new { ok = false, message = "Профиль друга не найден. Сначала создайте профиль." });

                if (infoDTO == null)
                    return BadRequest(new { ok = false, message = "Нет данных для обновления профиля" });

                string oldBio = friendProfile.Bio ?? "";
                string oldHobbies = friendProfile.Hobbies ?? "";
                decimal? oldRate = friendProfile.HourlyRate;
                string oldCity = friendProfile.City ?? "";
                int? oldAge = friendProfile.Age;

                if (infoDTO.Bio != null)
                    friendProfile.Bio = infoDTO.Bio;

                if (infoDTO.Hobbies != null)
                    friendProfile.Hobbies = infoDTO.Hobbies;

                if (infoDTO.HourlyRate.HasValue)
                {
                    if (infoDTO.HourlyRate <= 0)
                        return BadRequest(new { ok = false, message = "Почасовая ставка должна быть больше 0" });

                    friendProfile.HourlyRate = infoDTO.HourlyRate;
                }

                if (infoDTO.City != null)
                    friendProfile.City = infoDTO.City;

                if (infoDTO.Age.HasValue)
                {
                    if (infoDTO.Age < 18 || infoDTO.Age > 99)
                        return BadRequest(new { ok = false, message = "Возраст должен быть от 18 до 99 лет" });

                    friendProfile.Age = infoDTO.Age;
                }

                friendProfile.UpdatedAt = DateTime.UtcNow;
                var updateLog = new AuditLog(userId, "UPDATE_FRIEND_PROFILE", "FriendProfiles", friendProfile.ProfileID,
    $"Bio={oldBio}, Hobbies={oldHobbies}, HourlyRate={oldRate}, City={oldCity}, Age={oldAge}",
    $"Bio={friendProfile.Bio}, Hobbies={friendProfile.Hobbies}, HourlyRate={friendProfile.HourlyRate}, City={friendProfile.City}, Age={friendProfile.Age}",
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(updateLog);
                await _dbManager.SaveChangesAsync();

                var updatedProfile = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .FirstAsync(fp => fp.ProfileID == friendProfile.ProfileID);

                var profileDTO = FPInfoDTO.Convert(updatedProfile);

                return Ok(new { ok = true, message = "Профиль успешно обновлен", profile = profileDTO });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAll([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.FriendProfiles == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                    return NotFound(new { ok = false, message = "Пользователь не найден" });

                var profiles = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .Where(fp => fp.User != null && fp.User.IsActive && fp.User.Role == "Friend")
                    .OrderByDescending(fp => fp.AverageRating)
                    .ThenByDescending(fp => fp.CreatedAt)
                    .ToListAsync();

                var profilesDTO = profiles.Select(p => FPInfoDTO.Convert(p)).ToList();

                return Ok(new
                {
                    ok = true,
                    message = profilesDTO.Any() ? "Профили успешно получены" : "Профили не найдены",
                    count = profilesDTO.Count,
                    profiles = profilesDTO
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("myProfile")]
        [HttpGet]
        public async Task<ActionResult> GetMyProfile([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.FriendProfiles == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                    return NotFound(new { ok = false, message = "Пользователь не найден" });

                if (user.Role != "Friend")
                    return BadRequest(new { ok = false, message = "Только друзья имеют профиль" });

                var friendProfile = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .FirstOrDefaultAsync(fp => fp.UserID == userId);

                if (friendProfile == null)
                    return NotFound(new { ok = false, message = "Профиль не найден. Создайте профиль сначала." });

                var profileDTO = FPInfoDTO.Convert(friendProfile);

                return Ok(new { ok = true, message = "Профиль успешно получен", profile = profileDTO });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("profile/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetFriendProfileById(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId)
        {
            try
            {
                if (_dbManager?.FriendProfiles == null || _dbManager.Users == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var friendProfile = await _dbManager.FriendProfiles
                    .Include(fp => fp.User)
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId);

                if (friendProfile == null)
                    return NotFound(new { ok = false, message = "Профиль не найден" });

                if (friendProfile.User == null || !friendProfile.User.IsActive)
                    return NotFound(new { ok = false, message = "Профиль недоступен" });

                var profileDTO = FPInfoDTO.Convert(friendProfile);

                return Ok(new { ok = true, message = "Профиль успешно получен", profile = profileDTO });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("stats/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetFriendProfileStats(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId)
        {
            try
            {
                if (_dbManager?.FriendProfiles == null || _dbManager.Bookings == null || _dbManager.Reviews == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var profile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId);

                if (profile == null)
                    return NotFound(new { ok = false, message = "Профиль не найден" });

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

                return Ok(new { ok = true, message = "Статистика получена", statistics = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("upcomingMeetings/{profileId}")]
        [HttpGet]
        public async Task<ActionResult> GetUpcomingMeetings(
            [FromHeader(Name = "TOKEN")] string token,
            int profileId,
            [FromQuery] int top = 5)
        {
            try
            {
                if (_dbManager?.Bookings == null || _dbManager.Schedules == null || _dbManager.FriendProfiles == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var profile = await _dbManager.FriendProfiles
                    .FirstOrDefaultAsync(fp => fp.ProfileID == profileId);

                if (profile == null)
                    return NotFound(new { ok = false, message = "Профиль не найден" });

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
                    ok = true,
                    message = meetingsList.Any() ? "Ближайшие встречи получены" : "Нет ближайших встреч",
                    count = meetingsList.Count,
                    meetings = meetingsList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("cities")]
        [HttpGet]
        public async Task<ActionResult> GetAvailableCities([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.FriendProfiles == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var cities = await _dbManager.FriendProfiles
                    .Where(fp => fp.City != null && fp.City != "" && fp.IsVerified)
                    .Select(fp => fp.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(new { ok = true, message = "Города получены", count = cities.Count, cities = cities });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
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
                if (_dbManager?.FriendProfiles == null || _dbManager.Users == null ||
                    _dbManager.AuditLogs == null || _dbManager.Notifications == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { ok = false, message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Unauthorized(new { ok = false, message = "Требуются права администратора" });

                var profile = await _dbManager.FriendProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.ProfileID == profileId);

                if (profile == null)
                    return NotFound(new { ok = false, message = "Профиль не найден" });

                if (profile.User == null)
                    return StatusCode(500, new { ok = false, message = "Ошибка: пользователь профиля не найден" });
                if (string.IsNullOrWhiteSpace(data.VerificationNotes))
                {
                    data.VerificationNotes = data.IsVerified ? "Профиль был верифицирован" : "Верификация был отклонен по некоторым причинам";
                }
                profile.IsVerified = data.IsVerified;
                profile.VerificationNotes = data.VerificationNotes;

                _dbManager.AuditLogs.Add(new AuditLog(
                    adminId,
                    data.IsVerified ? "VERIFY_FRIEND_PROFILE" : "REJECT_FRIEND_VERIFICATION",
                    "FriendProfiles",
                    profileId,
                    $"IsVerified={!data.IsVerified}",
                    $"IsVerified={data.IsVerified}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    DateTime.UtcNow
                ));

                _dbManager.Notifications.Add(new Notification
                {
                    UserID = profile.UserID,
                    Title = data.IsVerified ? "✓ Профиль верифицирован" : "❌ Верификация отклонен",
                    Message = profile.VerificationNotes,
                    Type = "Verification",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    ok = true,
                    message = data.IsVerified ? "Профиль верифицирован" : "Верификация отклонена"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}