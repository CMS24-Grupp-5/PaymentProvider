using System.ComponentModel.DataAnnotations;

namespace Presentation.Data
{
    public class UserEntity
    {
        [Key]
        public string UserId { get; set; } = null!;
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        public ICollection<PaymentEntity>Payment {get;set;}=[];
    }
}
