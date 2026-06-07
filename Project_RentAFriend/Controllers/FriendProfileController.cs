using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
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
        public async Task<ActionResult> Create([FromBody] UserLoginDTO userLogin)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.FriendProfiles == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }
                var user = await _dbManager.Users.FirstOrDefaultAsync(u => u.UserID == userLogin.UserID);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}
