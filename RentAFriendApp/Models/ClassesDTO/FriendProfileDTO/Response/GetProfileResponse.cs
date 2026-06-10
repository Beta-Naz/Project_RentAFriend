namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetProfileResponse
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public FPInfoDTO Profile { get; set; }
    }
}
