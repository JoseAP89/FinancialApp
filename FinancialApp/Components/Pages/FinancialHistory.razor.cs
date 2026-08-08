using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using FinancialApp.Data.Models;
using FinancialApp.Data.Repositories;

namespace FinancialApp.Components.Pages
{
    public class FinancialHistoryBase : ComponentBase
    {
        [Inject]
        protected ITransactionRepository TransactionRepository { get; set; } = null!;

        [Inject]
        protected ILogger<FinancialHistoryBase> Logger { get; set; } = null!;

        protected IEnumerable<Transaction> FilteredTransactions { get; set; } = Enumerable.Empty<Transaction>();

        protected bool IsLoading { get; set; }
        protected decimal TotalTransactionValue { get; set; }

        // Bind to input type=date which uses yyyy-MM-dd format
        protected string? StartDateString { get; set; }
        protected string? EndDateString { get; set; }

        protected DateTime? StartDate => ParseDate(StartDateString);
        protected DateTime? EndDate => ParseDate(EndDateString);

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            try
            {
                // default dates
                StartDateString = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
                var end = DateTime.Today;
                // include entire day for end
                end = end.AddDays(1).AddTicks(-1);
                EndDateString = end.ToString("yyyy-MM-dd");
                var items = await TransactionRepository.ListWithNoSystemLinesByDateRangeAsync(StartDate!.Value, end);
                FilteredTransactions = items?.ToList() ?? [];
                TotalTransactionValue = GetTotalTransactionValue(FilteredTransactions); 
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to load transactions for FinancialHistory");
                FilteredTransactions = [];
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task ApplyFilter()
        {
            if (StartDate is null && EndDate is null)
            {
                FilteredTransactions = [];
                return;
            }

            var start = StartDate?.Date ?? DateTime.MinValue.Date;
            var end = EndDate?.Date ?? DateTime.Today;

            // include entire day for end
            end = end.AddDays(1).AddTicks(-1);

            var items = await TransactionRepository.ListWithNoSystemLinesByDateRangeAsync(start, end);
            FilteredTransactions = items?.ToList() ?? [];
            TotalTransactionValue = GetTotalTransactionValue(FilteredTransactions);
        }

        protected void ResetFilter()
        {
            StartDateString = null;
            EndDateString = null;
            FilteredTransactions = [];
            TotalTransactionValue = 0;
        }

        protected decimal GetTotalTransactionValue(IEnumerable<Transaction> transactions)
        {
            decimal total = 0;
            foreach (var transaction in transactions)
            {
                foreach (var line in transaction.TransactionLines)
                {
                    total += line.Amount;
                }
            }
            return total;
        }

        protected void OnStartDateChanged(ChangeEventArgs e)
        {
            StartDateString = Convert.ToString(e.Value);
        }

        protected void OnEndDateChanged(ChangeEventArgs e)
        {
            EndDateString = Convert.ToString(e.Value);
        }

        private DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var d)) return d;
            return null;
        }
    }
}
