using FinancialApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialApp.Data.Repositories
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(FinancialDbContext context) : base(context)
        {
        }

        public override async Task AddAsync(Transaction entity)
        {
            // Set server local time on create
            entity.Date = DateTime.Now;
            await base.AddAsync(entity);
        }

        public override void Update(Transaction entity)
        {
            // Update the date to server local time on update
            entity.Date = DateTime.Now;
            base.Update(entity);
        }

        // Override GetByIdAsync to include TransactionLines
        public override async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(t => t.TransactionLines)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // Override ListAsync to include TransactionLines
        public override async Task<IEnumerable<Transaction>> ListAsync()
        {
            return await _dbSet
                .Include(t => t.TransactionLines)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdWithLinesAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<IEnumerable<Transaction>> ListWithLinesAsync()
        {
            return await ListAsync();
        }

        public async Task<IEnumerable<Transaction>> ListWithLinesByDateRangeAsync(DateTime start, DateTime end)
        {
            // ensure end is inclusive
            var endInclusive = end;
            return await _dbSet
                .Include(t => t.TransactionLines)
                .Where(t => t.Date >= start && t.Date <= endInclusive)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }
    }
}
