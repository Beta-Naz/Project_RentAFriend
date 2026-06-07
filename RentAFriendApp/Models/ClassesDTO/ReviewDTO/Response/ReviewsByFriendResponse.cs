namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO.Response
{
    public class ReviewsByFriendResponse
    {
        public string Message { get; set; } = string.Empty;
        public decimal? AverageRating { get; set; }
        public List<ReviewDTO> Reviews { get; set; } = [];
    }
}
