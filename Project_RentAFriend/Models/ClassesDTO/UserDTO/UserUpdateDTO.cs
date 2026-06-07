using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.UserDTO
{
    public class UserMainInfoDTO
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
