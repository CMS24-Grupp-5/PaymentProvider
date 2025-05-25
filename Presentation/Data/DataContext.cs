using Microsoft.EntityFrameworkCore;

namespace Presentation.Data
{
    public class DataContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<PaymentEntity> Payments { get; set; }
        public DbSet<UserEntity> Users { get; set; }
    }
}
