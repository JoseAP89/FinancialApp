using FinancialApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<Account>> GetAllParentAccountsAsync()
        {
            return await _dbSet
                .Where(a => a.ParentId == null)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Account>> GetAllChildAccountsByParentIdAsync(int parentId)
        {
            return await _dbSet
                .Where(a => a.ParentId == parentId)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }
    }
}
