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
    public partial class PersonalFinance : ComponentBase
    {
        [Inject]
        protected IAccountRepository AccountRepository { get; set; } = null!;

        [Inject]
        protected ILogger<PersonalFinance> Logger { get; set; } = null!;

        protected List<Account> ParentAccounts { get; set; } = new List<Account>();

        protected List<Account> ChildAccounts { get; set; } = new List<Account>();

        private int? _selectedParentId;
        protected int? SelectedParentId
        {
            get => _selectedParentId;
            set
            {
                if (_selectedParentId != value)
                {
                    _selectedParentId = value;
                    _ = LoadChildAccountsForSelectedParentAsync(_selectedParentId);
                }
            }
        }

        protected int? SelectedChildId { get; set; }

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

        private async Task LoadChildAccountsForSelectedParentAsync(int? parentId)
        {
            SelectedChildId = null;
            if (parentId == null)
            {
                ChildAccounts = new List<Account>();
                await InvokeAsync(StateHasChanged);
                return;
            }

            try
            {
                var children = await AccountRepository.GetAllChildAccountsByParentIdAsync(parentId.Value);
                ChildAccounts = children?.ToList() ?? new List<Account>();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to load child accounts for parent id {ParentId}.", parentId);
                ErrorMessage = "Unable to load sub-accounts.";
                ChildAccounts = new List<Account>();
            }

            await InvokeAsync(StateHasChanged);
        }
    }
}
