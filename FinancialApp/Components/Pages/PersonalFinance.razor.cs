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

        protected int? SelectedParentId { get; set; }

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
    }
}
