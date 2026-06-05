using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;

namespace Project_RentAFriend.Controllers
{
    [Route("/user")]
    public class UserController : Controller
    {
        private DBManager? _dbManager;
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
                if(authUser == null || !PasswordHasher.Verify(password, authUser.PasswordHash))
                {
                    return Unauthorized(new { message = "Неверный email или пароль" });
                }
                string newToken = JwtToken.Generate(authUser);
                authUser.UpdatedAt = DateTime.UtcNow;
                authUser.IsActive = true;
                await _dbManager.SaveChangesAsync();
                return Ok(new 
                {
                    token = newToken,
                    role = authUser.Role
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}
