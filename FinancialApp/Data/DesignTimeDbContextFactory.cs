using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinancialApp.Data
{
    // Design-time factory to enable `dotnet ef` commands. It will create a local SQLite DB file named financialapp.db
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinancialDbContext>
    {
        public FinancialDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<FinancialDbContext>();
            var dataSource = "Data Source=financialapp.db";
            builder.UseSqlite(dataSource);
            return new FinancialDbContext(builder.Options);
        }
    }
}
