using FinancialApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialApp.Data
{
    public class FinancialDbContext : DbContext
    {
        public FinancialDbContext(DbContextOptions<FinancialDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
                entity.Property(a => a.Balance).HasColumnType("decimal(18,2)");
                entity.HasMany(a => a.Transactions).WithOne(t => t.Account!).HasForeignKey(t => t.AccountId);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
                entity.Property(t => t.Description).HasMaxLength(500);
            });
        }
    }
}
