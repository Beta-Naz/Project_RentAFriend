using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.UserDTO
{
    public class UserRegisterDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Client";
        public string? Phone { get; set; }
        public bool AgreeToTerms { get; set; }

    }
}
