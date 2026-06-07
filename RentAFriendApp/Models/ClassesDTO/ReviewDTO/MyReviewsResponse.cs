namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO
{
    public class MyReviewsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<MyReviewItem> Reviews { get; set; } = [];
    }
}
