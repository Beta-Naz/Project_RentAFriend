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

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <param name="email">Email пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>Токен авторизации и данные пользователя</returns>
        [Route("login")]
        [HttpPost]
        public async Task<ActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });
                }

                User? authUser = await _dbManager.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (authUser == null || !PasswordHasher.Verify(password, authUser.PasswordHash))
                {
                    var failedLog = new AuditLog(null, "LOGIN_FAILED", "Users", 0, null, $"Неудачная попытка входа с email: {email}",
                        HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                    _dbManager.AuditLogs.Add(failedLog);
                    await _dbManager.SaveChangesAsync();

                    return StatusCode(401, new { message = "Неверный email или пароль", statusCode = 401 });
                }

                if (!authUser.IsActive)
                {
                    return StatusCode(403, new { message = "Ваш аккаунт заблокирован. Обратитесь к администратору.", statusCode = 403 });
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
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Получение информации о текущем пользователе
        /// </summary>
        /// <param name="token">JWT токен пользователя</param>
        /// <returns>Данные пользователя</returns>
        [Route("get")]
        [HttpGet]
        public async Task<ActionResult> Get([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return StatusCode(401, new { message = "Недействительный токен", statusCode = 401 });
                }

                User? user = await _dbManager.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.UserID == userId);

                if (user == null)
                {
                    return StatusCode(404, new { message = "Пользователь не найден", statusCode = 404 });
                }

                if (user.IsActive == false)
                {
                    return StatusCode(403, new { message = "Пользователь заблокирован, обратитесь к администратору", statusCode = 403 });
                }

                return Ok(new { data = UserLoginDTO.Convert(user), statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="userRegister">Данные для регистрации</param>
        /// <returns>Результат регистрации</returns>
        [Route("create")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] UserRegisterDTO userRegister)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });
                }

                if (userRegister == null || !userRegister.AgreeToTerms)
                {
                    return StatusCode(400, new { message = "Ошибка регистрации", statusCode = 400 });
                }

                bool emailExists = await _dbManager.Users.AnyAsync(u => u.Email == userRegister.Email);
                if (emailExists)
                {
                    return StatusCode(409, new { message = "Электронная почта должна быть уникальной", statusCode = 409 });
                }

                if (userRegister.Role != "Client" && userRegister.Role != "Friend")
                {
                    return StatusCode(400, new { message = "Роль должна быть Client или Friend", statusCode = 400 });
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
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Проверка существования email
        /// </summary>
        /// <param name="email">Email для проверки</param>
        /// <returns>Результат проверки</returns>
        [Route("existsEmail")]
        [HttpPost]
        public async Task<ActionResult> ExistsEmail([FromForm] string email)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });
                }

                bool result = await _dbManager.Users.AnyAsync(u => u.Email == email);
                return Ok(new { result, statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Обновление информации о пользователе
        /// </summary>
        /// <param name="token">JWT токен пользователя</param>
        /// <param name="updateData">Данные для обновления</param>
        /// <returns>Результат обновления</returns>
        [Route("update")]
        [HttpPut]
        public async Task<ActionResult> Update([FromHeader(Name = "TOKEN")] string token, [FromBody] UserMainInfoDTO updateData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.AuditLogs == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return StatusCode(401, new { message = "Недействительный токен", statusCode = 401 });
                }

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null)
                {
                    return StatusCode(404, new { message = "Пользователь не найден", statusCode = 404 });
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

                return Ok(new { message = "Пользователь успешно обновлен", statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Получение всех пользователей (только для администратора)
        /// </summary>
        /// <param name="token">JWT токен администратора</param>
        /// <returns>Список всех пользователей</returns>
        [Route("getAll")]
        [HttpGet]
        public async Task<ActionResult> GetAllUsers([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return StatusCode(401, new { message = "Недействительный токен", statusCode = 401 });

                var user = await _dbManager.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                    return StatusCode(403, new { message = "Доступ запрещен. Требуются права администратора.", statusCode = 403 });

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

                return Ok(new { message = "Пользователи получены", users, statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Обновление статуса пользователя (блокировка/разблокировка) - только для администратора
        /// </summary>
        /// <param name="token">JWT токен администратора</param>
        /// <param name="userId">ID пользователя</param>
        /// <param name="isActive">Новый статус</param>
        /// <returns>Результат обновления статуса</returns>
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
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return StatusCode(401, new { message = "Недействительный токен", statusCode = 401 });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return StatusCode(403, new { message = "Доступ запрещен. Требуются права администратора.", statusCode = 403 });

                var targetUser = await _dbManager.Users
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(u => u.UserID == userId);

                if (targetUser == null)
                    return StatusCode(404, new { message = "Пользователь не найден", statusCode = 404 });

                if (string.IsNullOrEmpty(isActive))
                {
                    return StatusCode(400, new { message = "Некорректные данные", statusCode = 400 });
                }

                bool oldStatus = targetUser.IsActive;
                targetUser.IsActive = isActive != "false";
                var statusLog = new AuditLog(adminId, targetUser.IsActive ? "UNBLOCK_USER" : "BLOCK_USER", "Users", userId, oldStatus.ToString(), targetUser.IsActive.ToString(),
    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(statusLog);

                await _dbManager.SaveChangesAsync();

                return Ok(new { result = true, message = "Статус обновлен", statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Удаление пользователя (только для администратора)
        /// </summary>
        /// <param name="token">JWT токен администратора</param>
        /// <param name="userId">ID пользователя для удаления</param>
        /// <returns>Результат удаления</returns>
        [Route("delete/{userId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteUser(
            [FromHeader(Name = "TOKEN")] string token,
            int userId)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return StatusCode(401, new { message = "Недействительный токен", statusCode = 401 });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return StatusCode(403, new { message = "Доступ запрещен. Требуются права администратора.", statusCode = 403 });

                if (adminId == userId)
                    return StatusCode(400, new { message = "Нельзя удалить самого себя", statusCode = 400 });

                var targetUser = await _dbManager.Users
                             .IgnoreQueryFilters()
                             .FirstOrDefaultAsync(u => u.UserID == userId);

                if (targetUser == null)
                    return StatusCode(404, new { message = "Пользователь не найден", statusCode = 404 });

                var deleteLog = new AuditLog(adminId, "USER_DELETED", "Users", userId, $"Email={targetUser.Email}, FullName={targetUser.FullName}, Role={targetUser.Role}", "Пользователь удален",
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), DateTime.UtcNow);
                _dbManager.AuditLogs.Add(deleteLog);

                _dbManager.Users.Remove(targetUser);
                await _dbManager.SaveChangesAsync();

                return Ok(new { result = true, message = "Пользователь удален", statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Выход из системы (добавление токена в черный список)
        /// </summary>
        /// <param name="token">JWT токен для инвалидации</param>
        /// <returns>Результат выхода</returns>
        [Route("logout")]
        [HttpPost]
        public async Task<ActionResult> Logout([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.BlacklistedTokens == null || _dbManager.AuditLogs == null)
                    return StatusCode(500, new { message = "Ошибка базы данных", statusCode = 500 });

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                    return StatusCode(401, new { message = "Недействительный токен", statusCode = 401 });

                var expiresAt = JwtToken.GetExpirationDateFromToken(token);
                if (expiresAt == null)
                    return StatusCode(400, new { message = "Не удалось определить срок действия токена", statusCode = 400 });

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

                return Ok(new { message = "Выход выполнен успешно", statusCode = 200 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message, statusCode = 500 });
            }
        }
    }
}