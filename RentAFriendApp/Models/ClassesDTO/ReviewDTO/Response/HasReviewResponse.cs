using Newtonsoft.Json;

namespace RentAFriendApp.Models.ClassesDTO.ReviewDTO.Response
{
    public class HasReviewResponse
    {
        [JsonProperty("hasReview")]
        public bool HasReview { get; set; }
        [JsonProperty("reviewDto")]
        public ReviewDTO? Review { get; set; }
    }
}
