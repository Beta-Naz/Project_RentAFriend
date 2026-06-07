using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.UserDTO
{
    public class UserLoginDTO(int userID, string fullName, string role, bool isActive)
    {
        public int UserID { get; set; } = userID;
        public string FullName { get; set; } = fullName;
        public string Role { get; set; } = role;
        public bool IsActive { get; set; } = isActive;
    }
}
