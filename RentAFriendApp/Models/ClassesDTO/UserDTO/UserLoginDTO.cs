namespace RentAFriendApp.Models.ClassesDTO.UserDTO
{
    public class UserLoginDTO
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public UserLoginDTO(int userID, string fullName, string role, bool isActive)
        {
            UserID = userID;
            FullName = fullName;
            Role = role;
            IsActive = isActive;
        }
        public UserLoginDTO() {}
    }
}
