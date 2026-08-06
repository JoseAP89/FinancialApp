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
    public partial class AddTransaction : ComponentBase
    {
        [Inject]
        protected IAccountRepository AccountRepository { get; set; } = null!;

        [Inject]
        protected ILogger<AddTransaction> Logger { get; set; } = null!;

        protected List<Account> ParentAccounts { get; set; } = new List<Account>();

        protected List<Account> ChildAccounts { get; set; } = new List<Account>();

        protected int? SelectedChildId { get; set; }
        private string TransactionDescription { get; set; } = string.Empty;

        protected bool IsLoading { get; set; }

        protected string? ErrorMessage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            try
            {
                var parents = await AccountRepository.GetAllParentAccountsAsync();
                ParentAccounts = parents?.ToList() ?? new List<Account>();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Unable to load accounts. Please check the database connection.";
                Logger?.LogError(ex, "Failed to load parent accounts in PersonalFinance component.");
                ParentAccounts = new List<Account>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // UI transaction line DTO for building lines in the UI before mapping to Data.Models.TransactionLine
        protected class TransactionLineDto
        {
            public int? SelectedParentId { get; set; }
            public List<Account> ChildAccounts { get; set; } = new List<Account>();
            public int? SelectedChildId { get; set; }
            public string? Description { get; set; }
            public decimal Amount { get; set; }
        }

        protected List<TransactionLineDto> TransactionLines { get; set; } = new List<TransactionLineDto>();

        protected Transaction CurrentTransaction { get; set; } = new Transaction();

        protected void AddTransactionLine()
        {
            TransactionLines.Add(new TransactionLineDto());
        }

        protected void RemoveTransactionLine(TransactionLineDto line)
        {
            TransactionLines.Remove(line);
        }

        protected async Task OnParentChangedForLine(ChangeEventArgs e, TransactionLineDto line)
        {
            if (int.TryParse(Convert.ToString(e.Value), out var parentId))
            {
                line.SelectedParentId = parentId;
                await LoadChildAccountsForLineAsync(line, parentId);
            }
            else
            {
                line.SelectedParentId = null;
                line.ChildAccounts = new List<Account>();
                line.SelectedChildId = null;
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadChildAccountsForLineAsync(TransactionLineDto line, int parentId)
        {
            try
            {
                var children = await AccountRepository.GetAllChildAccountsByParentIdAsync(parentId);
                line.ChildAccounts = children?.ToList() ?? new List<Account>();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to load child accounts for parent id {ParentId} in line.", parentId);
                line.ChildAccounts = new List<Account>();
            }
        }

        // Helper: map current UI inputs into a Transaction model
        protected Transaction BuildTransactionFromInputs()
        {
            var tx = new Transaction
            {
                Description = TransactionDescription,
                Date = DateTime.UtcNow
            };

            foreach (var l in TransactionLines)
            {
                if (l.SelectedChildId.HasValue && l.Amount > 0)
                {
                    tx.TransactionLines.Add(new TransactionLine
                    {
                        AccountId = l.SelectedChildId.Value,
                        Amount = l.Amount,
                        Description = l.Description
                    });
                }
            }

            return tx;
        }

        protected void CreateTransaction()
        {
            var tx = BuildTransactionFromInputs();
            // Debug print of the built Transaction object
            Logger?.LogDebug("CreateTransaction debug: {@Transaction}", tx);
        }
    }
}
