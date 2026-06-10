using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.UserDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/user")]
    public class UserController : Controller
    {
        private readonly DBManager? _dbManager;
        public UserController()
        {
            _dbManager = new DBManager();
        }
        [Route("login")]
        [HttpPost]
        public async Task<ActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                User? authUser = await _dbManager.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (authUser == null || !PasswordHasher.Verify(password, authUser.PasswordHash))
                {
                    return Unauthorized(new { message = "Неверный email или пароль" });
                }
                if (!authUser.IsActive)
                {
                    return Unauthorized(new { message = "Ваш аккаунт заблокирован. Обратитесь к администратору." });
                }
                string newToken = JwtToken.Generate(authUser);
                await _dbManager.SaveChangesAsync();
                return Ok(new
                {
                    fullname = authUser.FullName,
                    token = newToken,
                    role = authUser.Role,
                    message = "Авторизация успешна"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("get")]
        [HttpGet]
        public async Task<ActionResult> Get([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }
                User? user = await _dbManager.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.UserID == userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "Пользователь не найден" });
                }
                if (user.IsActive == false)
                {
                    return Unauthorized(new { message = "Пользователь заблокирован, обратитесь к администратору" });
                }
                return Ok(UserLoginDTO.Convert(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] UserRegisterDTO userRegister)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                if(userRegister == null || !userRegister.AgreeToTerms)
                {
                    return Unauthorized(new { message = "Ошибка регистрации" });
                }

                bool emailExists = await _dbManager.Users.AnyAsync(u => u.Email == userRegister.Email);
                if (emailExists)
                {
                    return Conflict(new { message = "Электронная почта должна быть уникальной" });
                }
                if (userRegister.Role != "Client" && userRegister.Role != "Friend")
                {
                    return BadRequest(new { message = "Роль должна быть Client или Friend" });
                }
                User newUser = new()
                {
                    Email = userRegister.Email,
                    PasswordHash = PasswordHasher.Hash(userRegister.Password),
                    FullName = userRegister.FullName,
                    Role = userRegister.Role,
                    Phone = userRegister.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbManager.Users.Add(newUser);
                await _dbManager.SaveChangesAsync();
                return Ok(new
                {
                    message = "Пользователь успешно создан"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message});
            }
        }
        [Route("existsEmail")]
        [HttpPost]
        public async Task<ActionResult> ExistsEmail([FromForm] string email)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                bool result = await _dbManager.Users.AnyAsync(u => u.Email == email);
                return Ok(new {result});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("update")]
        [HttpPut]
        public async Task<ActionResult> Update([FromHeader(Name = "TOKEN")] 
                string token, [FromBody] UserMainInfoDTO updateData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }
                if (!string.IsNullOrWhiteSpace(updateData.FullName))
                    user.FullName = updateData.FullName;
                if (!string.IsNullOrWhiteSpace(updateData.Email))
                    user.Email = updateData.Email;
                if (updateData.Phone != null)
                    user.Phone = updateData.Phone;
                user.UpdatedAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();
                return Ok(new { message = "Пользователь успешно обновлен" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAllUsers([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                    return Forbid("Доступ запрещен");
                var users = await _dbManager.Users
                    .IgnoreQueryFilters()
                    .Select(u => new 
                    {
                        u.UserID,
                        u.FullName,
                        u.Email,
                        u.Phone,
                        u.Role,
                        u.IsActive,
                        u.CreatedAt,
                        u.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { message = "Пользователи получены", users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("updateStatus/{userId}")]
        [HttpPut]
        public async Task<ActionResult> UpdateUserStatus(
            [FromHeader(Name = "TOKEN")] string token,
            int userId,
            [FromForm] string isActive)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Forbid("Доступ запрещен");

                var targetUser = await _dbManager.Users.FindAsync(userId);
                if (targetUser == null)
                    return NotFound(new { message = "Пользователь не найден" });
                if (string.IsNullOrEmpty(isActive))
                {
                    return Forbid("Некорректные данные");
                }
                targetUser.IsActive = isActive != "false";
                _dbManager.AuditLogs.Add(new AuditLog
                {
                    UserID = adminId,
                    Action = targetUser.IsActive ? "UNBLOCK_USER" : "BLOCK_USER",
                    TableName = "Users",
                    RecordID = userId,
                    OldValue = (!targetUser.IsActive).ToString(),
                    NewValue = targetUser.IsActive.ToString(),
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    LoggedAt = DateTime.UtcNow
                });

                await _dbManager.SaveChangesAsync();
                return Ok(new { result = true, message = "Статус обновлен" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        [Route("delete/{userId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteUser(
            [FromHeader(Name = "TOKEN")] string token,
            int userId)
        {
            try
            {
                if (_dbManager?.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Forbid("Доступ запрещен");

                if (adminId == userId)
                    return BadRequest(new { message = "Нельзя удалить самого себя" });

                var targetUser = await _dbManager.Users.FindAsync(userId);
                if (targetUser == null)
                    return NotFound(new { message = "Пользователь не найден" });

                _dbManager.Users.Remove(targetUser);
                await _dbManager.SaveChangesAsync();

                return Ok(new { result = true, message = "Пользователь удален" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        [Route("logout")]
        [HttpPost]
        public async Task<ActionResult> Logout([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.BlacklistedTokens == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var expiresAt = JwtToken.GetExpirationDateFromToken(token);
                if (expiresAt == null)
                    return BadRequest(new { message = "Не удалось определить срок действия токена" });

                _dbManager.BlacklistedTokens.Add(new BlacklistedToken
                {
                    Token = token,
                    UserID = userId,
                    ExpiresAt = expiresAt.Value,
                    BlacklistedAt = DateTime.UtcNow
                });

                await _dbManager.SaveChangesAsync();

                return Ok(new { message = "Выход выполнен успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}
