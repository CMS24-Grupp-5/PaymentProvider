using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Data
{
    public class TicketEntity
    {
        [Key]
        public string TicketId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = null!; 

        [Required]
        [ForeignKey("Payment")]
        public string PaymentId { get; set; } = null!;

        public PaymentEntity Payment { get; set; } = null!;
    }
}
