using FinancialApp.Data.Models;
using System.Threading.Tasks;

namespace FinancialApp.Data.Repositories
{
    public interface IAccountRepository : IRepository<Account>
    {
        Task<Account?> GetByNameAsync(string name);
        Task<IEnumerable<Account>> GetAllParentAccountsAsync();
        Task<IEnumerable<Account>> GetAllChildAccountsByParentIdAsync(int parentId);
    }
}
