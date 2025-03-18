using Microsoft.EntityFrameworkCore;

    public class TransactionDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryRule> CategoryRules { get; set; }

        public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
            : base(options) { }
    }
