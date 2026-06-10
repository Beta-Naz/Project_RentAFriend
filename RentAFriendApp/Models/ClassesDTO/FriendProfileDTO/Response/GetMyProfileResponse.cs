namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetMyProfileResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Ok { get; set; } = false;
        public FPInfoDTO Profile { get; set; } = new();
    }
}
