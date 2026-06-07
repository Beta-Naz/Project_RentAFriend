namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetMyProfileResponse
    {
        public string Message { get; set; } = string.Empty;
        public FPInfoDTO Profile { get; set; } = new();
    }
}
