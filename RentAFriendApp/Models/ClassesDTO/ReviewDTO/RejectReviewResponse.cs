namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO
{
    public class RejectReviewResponse
    {
        public string Message { get; set; } = string.Empty;
        public int ReviewId { get; set; }
        public string? Reason { get; set; }
    }
}
