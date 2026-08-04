using FinancialApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FinancialApp.Data.Repositories
{
    public class AccountRepository : Repository<Account>, IAccountRepository
    {
        public AccountRepository(FinancialDbContext context) : base(context)
        {
        }

        public async Task<Account?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Name == name);
        }
    }
}
