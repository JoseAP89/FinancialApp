using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
// using System.Timers; (use fully-qualified name to avoid ambiguity with System.Threading.Timer)
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

        [Inject]
        protected ITransactionRepository TransactionRepository { get; set; } = null!;

        [Inject]
        protected Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = null!;

        protected List<Account> ParentAccounts { get; set; } = new List<Account>();

        protected List<Account> ChildAccounts { get; set; } = new List<Account>();

        private string TransactionDescription { get; set; } = string.Empty;

        protected bool IsLoading { get; set; }

        protected string? ErrorMessage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            try
            {
                var parents = await AccountRepository.GetVisibleParentAccountsAsync();
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
                // hide any child popover and stop its timer when selection cleared
                if (line.ChildPopoverTimer is not null)
                {
                    try { line.ChildPopoverTimer.Stop(); line.ChildPopoverTimer.Dispose(); } catch { }
                    line.ChildPopoverTimer = null;
                }
                line.IsChildPopoverVisible = false;
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
            public int Quantity { get; set; } = 1;
            // Popover UI state and timers
            public bool IsParentPopoverVisible { get; set; }
            public System.Timers.Timer? ParentPopoverTimer { get; set; }
            public bool IsChildPopoverVisible { get; set; }
            public System.Timers.Timer? ChildPopoverTimer { get; set; }
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
            && TransactionLines.All(l => 
                l.SelectedParentId.HasValue && 
                l.SelectedChildId.HasValue && 
                !string.IsNullOrWhiteSpace(l.Description) &&
                l.Amount > 0 &&
                l.Quantity >= 1);

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

                if (string.IsNullOrWhiteSpace(l.Description))
                {
                    errors.Add($"Line {lineIndex}: description must be selected.");
                }

                if (l.Amount <= 0)
                {
                    errors.Add($"Line {lineIndex}: amount must be greater than zero.");
                }

                if (l.Quantity < 1)
                {
                    errors.Add($"Line {lineIndex}: quantity must be at least 1.");
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
            // dispose any timers associated with the line to avoid leaks
            if (line.ParentPopoverTimer is not null)
            {
                try { line.ParentPopoverTimer.Stop(); line.ParentPopoverTimer.Dispose(); } catch { }
                line.ParentPopoverTimer = null;
            }
            if (line.ChildPopoverTimer is not null)
            {
                try { line.ChildPopoverTimer.Stop(); line.ChildPopoverTimer.Dispose(); } catch { }
                line.ChildPopoverTimer = null;
            }
            TransactionLines.Remove(line);
            ClearValidation();
        }

        protected async Task OnParentChangedForLine(ChangeEventArgs e, TransactionLineDto line)
        {
            if (int.TryParse(Convert.ToString(e.Value), out var parentId))
            {
                line.SelectedParentId = parentId;
                // When the parent account changes, clear the previously selected sub-account
                // so the user must explicitly choose a new sub-account for this line.
                line.SelectedChildId = null;
                // Also hide any child popover and stop its timer
                if (line.ChildPopoverTimer is not null)
                {
                    try { line.ChildPopoverTimer.Stop(); line.ChildPopoverTimer.Dispose(); } catch { }
                    line.ChildPopoverTimer = null;
                }
                line.IsChildPopoverVisible = false;
                await LoadChildAccountsForLineAsync(line, parentId);
            }
            else
            {
                line.SelectedParentId = null;
                line.ChildAccounts = new List<Account>();
                line.SelectedChildId = null;
                // hide both popovers and stop timers
                if (line.ParentPopoverTimer is not null)
                {
                    try { line.ParentPopoverTimer.Stop(); line.ParentPopoverTimer.Dispose(); } catch { }
                    line.ParentPopoverTimer = null;
                }
                line.IsParentPopoverVisible = false;

                if (line.ChildPopoverTimer is not null)
                {
                    try { line.ChildPopoverTimer.Stop(); line.ChildPopoverTimer.Dispose(); } catch { }
                    line.ChildPopoverTimer = null;
                }
                line.IsChildPopoverVisible = false;
            }

            // Clear validation display when the user edits inputs
            ClearValidation();
            await InvokeAsync(StateHasChanged);
        }

        // Toggle parent popover visibility. Clicking again will close. Auto-closes after 12s.
        protected void ToggleParentPopover(TransactionLineDto line)
        {
            if (line.IsParentPopoverVisible)
            {
                // close
                line.IsParentPopoverVisible = false;
                if (line.ParentPopoverTimer is not null)
                {
                    try { line.ParentPopoverTimer.Stop(); line.ParentPopoverTimer.Dispose(); } catch { }
                    line.ParentPopoverTimer = null;
                }
            }
            else
            {
                // open
                line.IsParentPopoverVisible = true;
                // ensure previous timer disposed
                if (line.ParentPopoverTimer is not null)
                {
                    try { line.ParentPopoverTimer.Stop(); line.ParentPopoverTimer.Dispose(); } catch { }
                    line.ParentPopoverTimer = null;
                }
                var t = new System.Timers.Timer(12000) { AutoReset = false };
                t.Elapsed += async (s, e) =>
                {
                    try
                    {
                        t.Stop();
                        t.Dispose();
                    }
                    catch { }
                    line.ParentPopoverTimer = null;
                    line.IsParentPopoverVisible = false;
                    await InvokeAsync(StateHasChanged);
                };
                line.ParentPopoverTimer = t;
                t.Start();
            }
        }

        // Toggle child popover visibility. Clicking again will close. Auto-closes after 12s.
        protected void ToggleChildPopover(TransactionLineDto line)
        {
            if (line.IsChildPopoverVisible)
            {
                line.IsChildPopoverVisible = false;
                if (line.ChildPopoverTimer is not null)
                {
                    try { line.ChildPopoverTimer.Stop(); line.ChildPopoverTimer.Dispose(); } catch { }
                    line.ChildPopoverTimer = null;
                }
            }
            else
            {
                line.IsChildPopoverVisible = true;
                if (line.ChildPopoverTimer is not null)
                {
                    try { line.ChildPopoverTimer.Stop(); line.ChildPopoverTimer.Dispose(); } catch { }
                    line.ChildPopoverTimer = null;
                }
                var t = new System.Timers.Timer(12000) { AutoReset = false };
                t.Elapsed += async (s, e) =>
                {
                    try
                    {
                        t.Stop();
                        t.Dispose();
                    }
                    catch { }
                    line.ChildPopoverTimer = null;
                    line.IsChildPopoverVisible = false;
                    await InvokeAsync(StateHasChanged);
                };
                line.ChildPopoverTimer = t;
                t.Start();
            }
        }

        private string GetParentDescription(TransactionLineDto line)
        {
            if (!line.SelectedParentId.HasValue) return string.Empty;
            var acct = ParentAccounts?.FirstOrDefault(a => a.Id == line.SelectedParentId.Value);
            return acct?.Description ?? string.Empty;
        }

        private string GetChildDescription(TransactionLineDto line)
        {
            if (!line.SelectedChildId.HasValue) return string.Empty;
            var acct = line.ChildAccounts?.FirstOrDefault(a => a.Id == line.SelectedChildId.Value);
            return acct?.Description ?? string.Empty;
        }

        private async Task LoadChildAccountsForLineAsync(TransactionLineDto line, int parentId)
        {
            try
            {
                var children = await AccountRepository.GetVisibleChildAccountsByParentIdAsync(parentId);
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
                        Description = l.Description,
                        Quantity = l.Quantity
                    });
                }
            }

            return tx;
        }

        // Auto-balance a transaction similar to the provided python implementation:
        // - Calculate current balance using account FinancialStatement:
        //   ASSET and EXPENSE contribute as debits (+), LIABILITY/EQUITY/REVENUE as credits (-)
        // - If the absolute balance > 0.01, find a cash/bank ASSET account and append a
        //   compensating TransactionLine with Amount = -balance and Description = "Auto-balance entry".
        protected async Task BalanceTransaction(Transaction transaction)
        {
            if (transaction == null) return;

            // Calculate current balance
            decimal balance = 0m;

            foreach (var line in transaction.TransactionLines)
            {
                try
                {
                    var acct = await AccountRepository.GetByIdAsync(line.AccountId);
                    if (acct is null)
                    {
                        Logger?.LogWarning("BalanceTransaction: account id {AccountId} not found; skipping line in balance calc.", line.AccountId);
                        continue;
                    }

                    var lineValue = line.Amount * (line.Quantity <= 0 ? 1 : line.Quantity);
                    if (acct.FinancialStatement == FinancialStatement.ASSET || acct.FinancialStatement == FinancialStatement.EXPENSE)
                    {
                        // Debit
                        balance += lineValue;
                    }
                    else
                    {
                        // Credit
                        balance -= lineValue;
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "BalanceTransaction: failed while reading account for line account id {AccountId}", line.AccountId);
                }
            }

            // If not balanced beyond threshold, add compensating line
            if (Math.Abs(balance) > 0.01m)
            {
                try
                {
                    // Try to find a suitable cash/bank asset account by listing accounts and searching names
                    var allAccounts = (await AccountRepository.ListAsync())?.ToList() ?? new List<Account>();
                    var checkAccount = allAccounts
                        .FirstOrDefault(a => 
                            a.FinancialStatement == FinancialStatement.ASSET
                            && (a.Name?.IndexOf("Checking Account", StringComparison.OrdinalIgnoreCase) >= 0)
                        );
                    if (checkAccount is null)
                    {
                        Logger?.LogWarning("BalanceTransaction: no cash/bank ASSET account found to auto-balance transaction (balance={Balance}).", balance);
                        return;
                    }

                    var compensating = new TransactionLine
                    {
                        AccountId = checkAccount.Id,
                        Amount = decimal.Round(-balance, 2, MidpointRounding.ToEven),
                        Description = "Auto-balance entry"
                    };
                    compensating.Quantity = 1;

                    transaction.TransactionLines.Add(compensating);
                    Logger?.LogInformation("BalanceTransaction: added auto-balance line to account id {AccountId} amount {Amount}.", compensating.AccountId, compensating.Amount);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "BalanceTransaction: failed to add compensating line for balance {Balance}", balance);
                }
            }
        }

        protected async Task CreateTransaction()
        {
            // Show validation errors after the user clicked submit
            ShowValidation = true;

            if (!CanSubmit)
            {
                Logger?.LogWarning("CreateTransaction aborted: validation failed. Errors: {Errors}", string.Join("; ", GetValidationErrors()));
                return;
            }

            var tx = BuildTransactionFromInputs();
            await BalanceTransaction(tx);
            Logger?.LogDebug("CreateTransaction debug: {@Transaction}", tx);

            try
            {
                IsLoading = true;

                // Persist transaction (EF will cascade insert TransactionLines attached to the Transaction)
                await TransactionRepository.AddAsync(tx);
                await TransactionRepository.SaveChangesAsync();

                // Show success toast (JS alert fallback)
                try { await JSRuntime.InvokeAsync<object>("alert", new object?[] { "Transaction added successfully" }); } catch { }
                ClearTransactionPageState();

            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "CreateTransaction failed while saving transaction.");
                try { await JSRuntime.InvokeAsync<object>("alert", new object?[] { "There was an error with your request, try again." }); } catch { }
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void ClearTransactionPageState()
        {
            // Reset UI state
            ClearValidation();
            TransactionLines.Clear();
            TransactionDescription = string.Empty;
            CurrentTransaction = new Transaction();
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

        protected async Task OnLineQuantityChanged(ChangeEventArgs e, TransactionLineDto line)
        {
            var s = Convert.ToString(e.Value);
            if (int.TryParse(s, out var quantity))
            {
                line.Quantity = quantity;
            }
            else
            {
                line.Quantity = 1;
            }

            ClearValidation();
            await InvokeAsync(StateHasChanged);
        }
    }
}
