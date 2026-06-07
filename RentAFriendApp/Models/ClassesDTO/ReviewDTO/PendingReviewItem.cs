namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO
{
    public class PendingReviewItem
    {
        public int ReviewID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int BookingID { get; set; }
        public int FriendProfileID { get; set; }
    }
}
