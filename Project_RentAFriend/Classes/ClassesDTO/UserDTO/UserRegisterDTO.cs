using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.UserDTO
{
    public class UserRegisterDTO
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = "Client";
        public string? Phone { get; set; }
        public bool AgreeToTerms { get; set; }

    }
}
