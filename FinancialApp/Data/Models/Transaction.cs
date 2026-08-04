using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApp.Data.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account? Account { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
