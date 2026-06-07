namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO
{
    public class PendingReviewsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<PendingReviewItem> Reviews { get; set; } = [];
    }
}
