using Microsoft.EntityFrameworkCore;

namespace Laboration2_MVC.Models
{

    public class TransactionDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<CategoryRule> CategoryRules { get; set; }

        public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
            : base(options)
        {
        }
    }
}
