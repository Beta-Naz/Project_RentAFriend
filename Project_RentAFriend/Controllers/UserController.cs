using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Context;
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

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        [Route("login")]
        [HttpPost]
        public async Task<ActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });
                }

                User? authUser = await _dbManager.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (authUser == null || !PasswordHasher.Verify(password, authUser.PasswordHash))
                {
                    var failedLog = new AuditLog(null, "LOGIN_FAILED", "Users", 0, null, $"Неудачная попытка входа с email: {email}",
                        HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                    _dbManager.AuditLogs.Add(failedLog);
                    await _dbManager.SaveChangesAsync();

                    return StatusCode(401, new { message = "Неверный email или пароль", ok = false });
                }

                if (!authUser.IsActive)
                {
                    return StatusCode(403, new { message = "Ваш аккаунт заблокирован. Обратитесь к администратору.", ok = false });
                }

                string newToken = JwtToken.Generate(authUser);

                var successLog = new AuditLog(authUser.UserID, "LOGIN_SUCCESS", "Users", authUser.UserID, null, $"Успешный вход пользователя {authUser.Email}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(successLog);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    fullname = authUser.FullName,
                    token = newToken,
                    role = authUser.Role,
                    message = "Авторизация успешна",
                    ok = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Получение информации о текущем пользователе
        /// </summary>
        [Route("get")]
        [HttpGet]
        public async Task<ActionResult> Get([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                using var dbManager = new DBManager();
                if (dbManager == null || dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });
                }
                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return StatusCode(401, new { message = "Недействительный токен", ok = false });
                }
                if (dbManager.BlacklistedTokens != null)
                {
                    foreach (var backToken in dbManager.BlacklistedTokens)
                    {
                        if (backToken.Token == token)
                        {
                            return StatusCode(401, new { message = "Недействительный токен", ok = false });
                        }
                    }
                }
                User? user = await dbManager.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.UserID == userId);

                if (user == null)
                {
                    return StatusCode(404, new { message = "Пользователь не найден", ok = false });
                }

                if (user.IsActive == false)
                {
                    return StatusCode(403, new { message = "Пользователь заблокирован, обратитесь к администратору", ok = false });
                }

                return Ok(new { data = UserLoginDTO.Convert(user), ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] UserRegisterDTO userRegister)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });
                }

                if (userRegister == null || !userRegister.AgreeToTerms)
                {
                    return StatusCode(400, new { message = "Ошибка регистрации", ok = false });
                }

                bool emailExists = await _dbManager.Users.AnyAsync(u => u.Email == userRegister.Email);
                if (emailExists)
                {
                    return StatusCode(409, new { message = "Электронная почта должна быть уникальной", ok = false });
                }

                if (userRegister.Role != "Client" && userRegister.Role != "Friend")
                {
                    return StatusCode(400, new { message = "Роль должна быть Client или Friend", ok = false });
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

                var regLog = new AuditLog(newUser.UserID, "USER_REGISTERED", "Users", newUser.UserID, null, $"Зарегистрирован новый пользователь: Email={newUser.Email}, Role={newUser.Role}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(regLog);
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Пользователь успешно создан",
                    ok = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Проверка существования email
        /// </summary>
        [Route("existsEmail")]
        [HttpPost]
        public async Task<ActionResult> ExistsEmail([FromForm] string email)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });
                }
                bool result = await _dbManager.Users.AnyAsync(u => u.Email == email);
                return Ok(new { ok = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Обновление информации о пользователе
        /// </summary>
        [Route("update")]
        [HttpPut]
        public async Task<ActionResult> Update([FromHeader(Name = "TOKEN")] string token, [FromBody] UserMainInfoDTO updateData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return StatusCode(401, new { message = "Недействительный токен", ok = false });
                }

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null)
                {
                    return StatusCode(404, new { message = "Пользователь не найден", ok = false });
                }

                string oldValues = $"FullName={user.FullName}, Email={user.Email}, Phone={user.Phone}";

                if (!string.IsNullOrWhiteSpace(updateData.FullName))
                    user.FullName = updateData.FullName;
                if (!string.IsNullOrWhiteSpace(updateData.Email))
                    user.Email = updateData.Email;
                if (updateData.Phone != null)
                    user.Phone = updateData.Phone;

                user.UpdatedAt = DateTime.UtcNow;

                string newValues = $"FullName={user.FullName}, Email={user.Email}, Phone={user.Phone}";

                var updateLog = new AuditLog(userId, "USER_UPDATED", "Users", userId.Value, oldValues, newValues,
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(updateLog);

                await _dbManager.SaveChangesAsync();

                return Ok(new { message = "Пользователь успешно обновлен", ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Получение всех пользователей (только для администратора)
        /// </summary>
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAllUsers([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return StatusCode(401, new { message = "Недействительный токен", ok = false });

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                    return StatusCode(403, new { message = "Доступ запрещен. Требуются права администратора.", ok = false });

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

                return Ok(new { message = "Пользователи получены", users, ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Обновление статуса пользователя (блокировка/разблокировка) - только для администратора
        /// </summary>
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
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return StatusCode(401, new { message = "Недействительный токен", ok = false });

                var admin = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == adminId);
                if (admin == null || admin.Role != "Admin")
                    return StatusCode(403, new { message = "Доступ запрещен. Требуются права администратора.", ok = false });

                var targetUser = await _dbManager.Users
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(u => u.UserID == userId);

                if (targetUser == null)
                    return StatusCode(404, new { message = "Пользователь не найден", ok = false });

                if (string.IsNullOrEmpty(isActive))
                {
                    return StatusCode(400, new { message = "Некорректные данные", ok = false });
                }

                bool oldStatus = targetUser.IsActive;
                targetUser.IsActive = isActive != "false";
                var statusLog = new AuditLog(adminId, targetUser.IsActive ? "UNBLOCK_USER" : "BLOCK_USER", "Users", userId, oldStatus.ToString(), targetUser.IsActive.ToString(),
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(statusLog);

                await _dbManager.SaveChangesAsync();

                return Ok(new { result = true, message = "Статус обновлен", ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Удаление пользователя (только для администратора)
        /// </summary>
        [Route("delete/{userId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteUser(
            [FromHeader(Name = "TOKEN")] string token,
            int userId)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return StatusCode(401, new { message = "Недействительный токен", ok = false });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return StatusCode(403, new { message = "Доступ запрещен. Требуются права администратора.", ok = false });

                if (adminId == userId)
                    return StatusCode(400, new { message = "Нельзя удалить самого себя", ok = false });

                var targetUser = await _dbManager.Users
                             .IgnoreQueryFilters()
                             .FirstOrDefaultAsync(u => u.UserID == userId);

                if (targetUser == null)
                    return StatusCode(404, new { message = "Пользователь не найден", ok = false });

                var deleteLog = new AuditLog(adminId, "USER_DELETED", "Users", userId, $"Email={targetUser.Email}, FullName={targetUser.FullName}, Role={targetUser.Role}", "Пользователь удален",
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(deleteLog);

                _dbManager.Users.Remove(targetUser);
                await _dbManager.SaveChangesAsync();

                return Ok(new { result = true, message = "Пользователь удален", ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }

        /// <summary>
        /// Выход из системы (добавление токена в черный список)
        /// </summary>
        [Route("logout")]
        [HttpPost]
        public async Task<ActionResult> Logout([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.BlacklistedTokens == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { message = "Ошибка базы данных", ok = false });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return StatusCode(401, new { message = "Недействительный токен", ok = false });

                var expiresAt = JwtToken.GetExpirationDateFromToken(token);
                if (expiresAt == null)
                    return StatusCode(400, new { message = "Не удалось определить срок действия токена", ok = false });

                _dbManager.BlacklistedTokens.Add(new BlacklistedToken
                {
                    Token = token,
                    UserID = userId,
                    ExpiresAt = expiresAt.Value,
                    BlacklistedAt = DateTime.UtcNow
                });

                var logoutLog = new AuditLog(userId, "LOGOUT", "BlacklistedTokens", userId.Value, null, "Пользователь вышел из системы",
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(logoutLog);

                await _dbManager.SaveChangesAsync();

                return Ok(new { message = "Выход выполнен успешно", ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, ok = false });
            }
        }
    }
}