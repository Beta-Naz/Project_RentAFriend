namespace RentAFriendApp.Models.ClassesDTO.UserDTO.Response
{
    public class GetAllUsersResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<UserInfoDTO> Users { get; set; } = [];
    }
}