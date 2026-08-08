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
                .AsNoTracking()
                .Include(t => t.TransactionLines)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // Override ListAsync to include TransactionLines
        public override async Task<IEnumerable<Transaction>> ListAsync()
        {
            return await _dbSet
                .AsNoTracking()
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

        public async Task<IEnumerable<Transaction>> ListWithNoSystemLinesByDateRangeAsync(DateTime start, DateTime end)
        {
            // ensure end is inclusive
            var endInclusive = end;
            // Include only transaction lines whose related account is not a system account
            // Use AsNoTracking to ensure EF materializes fresh instances from the database
            // so previously tracked TransactionLines won't be reused by the change tracker.
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.Date >= start && t.Date <= endInclusive)
                .Include(t => t.TransactionLines.Where(l => l.Account != null && !l.Account.IsSystem))
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }
    }
}
