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
                User? user = await _dbManager.Users
                    .FirstOrDefaultAsync(x => x.UserID == userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "Пользователь не найден" });
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
    }
}
