namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class CreateProfileResponse
    {
        public string Message { get; set; } = string.Empty;
        public FPInfoDTO FriendProfile { get; set; } = new();
    }
}
