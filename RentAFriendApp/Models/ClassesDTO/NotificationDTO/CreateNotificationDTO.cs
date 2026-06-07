using System.ComponentModel.DataAnnotations;
namespace RentAFriendApp.Models.ClassesDTO.NotificationDTO
{
    public class CreateNotificationDTO
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "Info";
    }
}