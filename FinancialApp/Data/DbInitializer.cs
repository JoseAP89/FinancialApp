using System.Linq;
using FinancialApp.Data.Models;

namespace FinancialApp.Data
{
    public static class DbInitializer
    {
        public static void Seed(FinancialDbContext context)
        {
            // Simple idempotent seed
            if (context.Accounts.Any())
                return;

            var acct = new Account
            {
                Name = "Checking",
                Description = "Primary checking account",
                FinancialStatement = FinancialStatement.ASSET
            };

            context.Accounts.Add(acct);
            // No direct relationship between Account and Transaction, so do not create account-linked transactions here.

            context.SaveChanges();
        }
    }
}
