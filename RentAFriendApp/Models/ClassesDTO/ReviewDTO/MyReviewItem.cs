namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO
{
    public class MyReviewItem
    {
        public int ReviewID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FriendName { get; set; } = string.Empty;
        public int FriendProfileID { get; set; }
    }
}
