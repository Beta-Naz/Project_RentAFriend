using Project_RentAFriend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Message
{
    [Key]
    public int MessageID { get; set; }

    [Required]
    public int ChatID { get; set; }

    [Required]
    public int SenderID { get; set; }

    public int? BookingID { get; set; }

    [Required]
    [MaxLength(20)]
    public string MessageType { get; set; } = "Text";

    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, 10485760)]
    public int? FileSize { get; set; }

    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    public bool IsRead { get; set; } = false;

    public bool IsEdited { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(ChatID))]
    public virtual Chat? Chat { get; set; }

    [ForeignKey(nameof(SenderID))]
    public virtual User? Sender { get; set; }

    [ForeignKey(nameof(BookingID))]
    public virtual Booking? Booking { get; set; }
}
}