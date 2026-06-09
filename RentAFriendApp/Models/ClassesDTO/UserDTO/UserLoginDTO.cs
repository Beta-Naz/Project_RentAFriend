namespace RentAFriendApp.Models.ClassesDTO.UserDTO
{
    public class UserLoginDTO
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public UserLoginDTO(int userID, string fullName, string role, string email, string phone, bool isActive)
        {
            UserID = userID;
            FullName = fullName;
            Role = role;
            Email = email;
            Phone = phone;
            IsActive = isActive;
        }

        public UserLoginDTO() {}
    }
}
