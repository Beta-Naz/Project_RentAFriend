namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO
{
    public class ReviewDTO
    {
        public int ReviewID { get; set; }
        public string? Title { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? ModeratorComment { get; set; }
    }
}