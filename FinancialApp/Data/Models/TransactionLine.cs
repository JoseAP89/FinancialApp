using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApp.Data.Models
{
    public class TransactionLine
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to Transaction
        public int TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        // Foreign key to Account
        public int AccountId { get; set; }
        public Account? Account { get; set; }

        // Amount for this line
        public decimal Amount { get; set; }

        // Optional description for the transaction line
        public string? Description { get; set; }
    }
}
