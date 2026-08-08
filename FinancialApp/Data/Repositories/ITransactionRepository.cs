using FinancialApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialApp.Data.Repositories
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        // Returns a transaction including its transaction lines
        Task<Transaction?> GetByIdWithLinesAsync(int id);

        // Returns all transactions including their transaction lines
        Task<IEnumerable<Transaction>> ListWithLinesAsync();

        // Returns transactions including their lines between the given start and end (inclusive)
        Task<IEnumerable<Transaction>> ListWithLinesByDateRangeAsync(DateTime start, DateTime end);
    }
}
