namespace Project_RentAFriend.Models.ClassesDTO.ReviewDTO
{
    public class ReviewDTO
    {
        public int ReviewID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? ModeratorComment { get; set; }

        public static ReviewDTO Convert(Review review)
        {
            return new ReviewDTO
            {
                ReviewID = review.ReviewID,
                Rating = review.Rating,
                Comment = review.Comment,
                IsApproved = review.IsApproved,
                CreatedAt = review.CreatedAt,
                ClientName = review.Booking?.Client?.FullName ?? "Unknown",
                ModeratorComment = review.ModeratorComment
            };
        }
    }
}