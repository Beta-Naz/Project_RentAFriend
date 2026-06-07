using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.FriendProfileDTO;
using Project_RentAFriend.Models.ClassesDTO.UserDTO;

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
        public async Task<ActionResult> Create([FromHeader(Name = "TOKEN")] string token, FPMainInfoDTO infoDTO)
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
                FriendProfile friendProfile = new()
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
                _dbManager.FriendProfiles.Add(friendProfile);
                await _dbManager.SaveChangesAsync();
                return Ok(new 
                { 
                    message = "Профиль успешно создан",
                    friendProfile 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("update")]
        [HttpPut]
        public async Task<ActionResult> Update([FromHeader(Name = "TOKEN")] string token, FPMainInfoDTO infoDTO)
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
    }
}
