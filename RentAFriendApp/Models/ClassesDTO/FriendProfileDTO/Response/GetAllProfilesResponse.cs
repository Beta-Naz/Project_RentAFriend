namespace RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response
{
    public class GetAllProfilesResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<FPInfoDTO> Profiles { get; set; } = [];
    }
}
