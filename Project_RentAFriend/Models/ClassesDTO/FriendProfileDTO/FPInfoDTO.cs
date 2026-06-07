namespace Project_RentAFriend.Models.ClassesDTO.FriendProfileDTO
{
    public class FPInfoDTO
    {
        public int ProfileID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public int? Age { get; set; }
        public string? City { get; set; }
        public string? Hobbies { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? AverageRating { get; set; }
        public bool IsVerified { get; set; }
        public static FPInfoDTO Convert(FriendProfile profile)
        {
            return new FPInfoDTO
            {
                ProfileID = profile.ProfileID,
                FullName = profile.User?.FullName ?? string.Empty,
                Bio = profile.Bio,
                Age = profile.Age,
                City = profile.City,
                Hobbies = profile.Hobbies,
                HourlyRate = profile.HourlyRate,
                AverageRating = profile.AverageRating,
                IsVerified = profile.IsVerified
            };
        }
    }
}
