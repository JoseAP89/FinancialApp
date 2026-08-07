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

        protected async Task OnChildChangedForLine(ChangeEventArgs e, TransactionLineDto line)
        {
            if (int.TryParse(Convert.ToString(e.Value), out var id))
            {
                line.SelectedChildId = id;
            }
            else
            {
                line.SelectedChildId = null;
            }

            // Clear validation display when the user edits inputs
            ClearValidation();
            await InvokeAsync(StateHasChanged);
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

        // Controls whether validation errors are shown after the user attempts to submit
        protected bool ShowValidation { get; set; }

        protected void ClearValidation()
        {
            ShowValidation = false;
            _ = InvokeAsync(StateHasChanged);
        }

        // Validation: only allow submit when transaction description present, at least one line,
        // and every line has selected parent & child account and amount > 0
        protected bool CanSubmit =>
            !string.IsNullOrWhiteSpace(TransactionDescription)
            && TransactionLines is not null
            && TransactionLines.Any()
            && TransactionLines.All(l => l.SelectedParentId.HasValue && l.SelectedChildId.HasValue && l.Amount > 0);

        protected IEnumerable<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TransactionDescription))
            {
                errors.Add("Transaction description is required.");
            }

            if (TransactionLines is null || !TransactionLines.Any())
            {
                errors.Add("At least one transaction line is required.");
                return errors;
            }

            for (var i = 0; i < TransactionLines.Count; i++)
            {
                var l = TransactionLines[i];
                var lineIndex = i + 1;

                if (!l.SelectedParentId.HasValue)
                {
                    errors.Add($"Line {lineIndex}: account (parent) must be selected.");
                }

                if (!l.SelectedChildId.HasValue)
                {
                    errors.Add($"Line {lineIndex}: sub-account must be selected.");
                }

                if (l.Amount <= 0)
                {
                    errors.Add($"Line {lineIndex}: amount must be greater than zero.");
                }
            }

            return errors;
        }

        protected void AddTransactionLine()
        {
            TransactionLines.Add(new TransactionLineDto());
            ClearValidation();
        }

        protected void RemoveTransactionLine(TransactionLineDto line)
        {
            TransactionLines.Remove(line);
            ClearValidation();
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

            // Clear validation display when the user edits inputs
            ClearValidation();
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
            // Show validation errors after the user clicked submit
            ShowValidation = true;

            if (!CanSubmit)
            {
                Logger?.LogWarning("CreateTransaction aborted: validation failed. Errors: {Errors}", string.Join("; ", GetValidationErrors()));
                return;
            }

            var tx = BuildTransactionFromInputs();
            Logger?.LogDebug("CreateTransaction debug: {@Transaction}", tx);
            // After successful creation, clear validation display
            ClearValidation();
        }

        protected void OnTransactionDescriptionChanged(ChangeEventArgs e)
        {
            TransactionDescription = Convert.ToString(e.Value) ?? string.Empty;
            ClearValidation();
        }

        protected async Task OnLineDescriptionChanged(ChangeEventArgs e, TransactionLineDto line)
        {
            line.Description = Convert.ToString(e.Value);
            ClearValidation();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnLineAmountChanged(ChangeEventArgs e, TransactionLineDto line)
        {
            var s = Convert.ToString(e.Value);
            if (decimal.TryParse(s, out var amount))
            {
                line.Amount = amount;
            }
            else
            {
                line.Amount = 0;
            }

            ClearValidation();
            await InvokeAsync(StateHasChanged);
        }
    }
}
