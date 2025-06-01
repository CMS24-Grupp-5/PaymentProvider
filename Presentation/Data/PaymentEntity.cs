using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Presentation.Data
{
    public class PaymentEntity
    {
        [Key]
        public string PaymentId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string EventId { get; set; } = null!;

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string StripeSessionId { get; set; } = null!;

        public bool IsPaid { get; set; } = false;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; } = null!;

        public UserEntity User { get; set; } = null!;

        public ICollection<TicketEntity> Tickets { get; set; } = new List<TicketEntity>();
    }
}
