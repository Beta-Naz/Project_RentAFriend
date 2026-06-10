using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;

namespace Project_RentAFriend.Controllers
{
    [Route("/admin")]
    public class AdminController : Controller
    {
        private readonly DBManager? _dbManager;

        public AdminController()
        {
            _dbManager = new DBManager();
        }

        [Route("statistics")]
        [HttpGet]
        public async Task<ActionResult> GetStatistics([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager?.Users == null || _dbManager.Bookings == null || _dbManager.FriendProfiles == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Forbid("Доступ запрещен");

                var totalUsers = await _dbManager.Users.CountAsync();
                var activeUsers = await _dbManager.Users.CountAsync(u => u.IsActive);
                var blockedUsers = await _dbManager.Users.CountAsync(u => !u.IsActive);
                var totalBookings = await _dbManager.Bookings.CountAsync();
                var totalRevenue = await _dbManager.Bookings
                    .Where(b => b.PaymentStatus == "Paid")
                    .SumAsync(b => b.TotalAmount);
                var pendingVerifications = await _dbManager.FriendProfiles
                    .CountAsync(fp => !fp.IsVerified);

                return Ok(new
                {
                    totalUsers,
                    activeUsers,
                    blockedUsers,
                    totalBookings,
                    totalRevenue,
                    pendingVerifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}