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
        public DbSet<TransactionLine> TransactionLines { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ACCOUNT ENTITY CONFIGURATION
            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
                // Account.Description is a string stored as TEXT in SQLite
                entity.Property(a => a.Description).HasColumnType("TEXT");
                // Store FinancialStatement enum as its string name (e.g., "ASSET") in the database
                entity.Property(a => a.FinancialStatement)
                      .HasConversion<string>()
                      .HasColumnType("TEXT");

                // Self-referencing one-to-many: an Account can have a Parent (nullable) and many Children
                entity.HasOne(a => a.Parent)
                      .WithMany(a => a.Children)
                      .HasForeignKey(a => a.ParentId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // TRANSACTION ENTITY CONFIGURATION
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Description).HasMaxLength(500);
            });

            // TRANSACTION LINE ENTITY CONFIGURATION
            modelBuilder.Entity<TransactionLine>(entity =>
            {
                entity.HasKey(tl => tl.Id);
                entity.Property(tl => tl.Amount).HasColumnType("decimal(18,2)");
                // TransactionLine.Description is a string stored as TEXT in SQLite
                entity.Property(a => a.Description).HasColumnType("TEXT");

                // Quantity: integer, required, default 1 and check constraint to ensure >= 1
                entity.Property(tl => tl.Quantity)
                      .IsRequired()
                      .HasDefaultValue(1);

                entity.ToTable(t => t.HasCheckConstraint("CK_TransactionLines_Quantity", "Quantity >= 1"));

                // TransactionLine -> Transaction (many lines per transaction)
                entity.HasOne(tl => tl.Transaction)
                      .WithMany(t => t.TransactionLines)
                      .HasForeignKey(tl => tl.TransactionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // TransactionLine -> Account (many lines per account)
                entity.HasOne(tl => tl.Account)
                      .WithMany(a => a.TransactionLines)
                      .HasForeignKey(tl => tl.AccountId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
