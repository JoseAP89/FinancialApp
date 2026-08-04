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
                // Account.Description is a string stored as TEXT in SQLite
                entity.Property(a => a.Description).HasColumnType("TEXT");
                // Store FinancialStatement enum as its string name (e.g., "ASSET") in the database
                entity.Property(a => a.FinancialStatement)
                      .HasConversion<string>()
                      .HasColumnType("TEXT");
                entity.HasMany(a => a.Transactions).WithOne(t => t.Account!).HasForeignKey(t => t.AccountId);

                // Self-referencing one-to-many: an Account can have a Parent (nullable) and many Children
                entity.HasOne(a => a.Parent)
                      .WithMany(a => a.Children)
                      .HasForeignKey(a => a.ParentId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
                entity.Property(t => t.Description).HasMaxLength(500);
            });
        }
    }
}
