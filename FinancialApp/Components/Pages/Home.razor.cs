using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FinancialApp.Components.Pages
{
    public partial class Home : ComponentBase, IDisposable
    {
        protected LoanModel Model { get; set; } = new LoanModel();
        protected EditContext EditContext { get; set; }
        protected string ResultText { get; set; } = string.Empty;

        protected override void OnInitialized()
        {
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += HandleFieldChanged;
            UpdateResult();
        }

        private void HandleFieldChanged(object sender, FieldChangedEventArgs e)
        {
            // react to every field change (similar to reactive forms)
            UpdateResult();
        }

        protected void HandleValidSubmit()
        {
            UpdateResult();
        }

        private void UpdateResult()
        {
            if (Model.Amount <= 0 || Model.Period <= 0)
            {
                ResultText = string.Empty;
                return;
            }
            // Future value of an annuity formula
            // r: interest rate per period (use monthly rate)
            decimal r = Model.AnnualInterestRate / 100m / 12m;
            int n = Model.Period; // Period is in months
            decimal pv = Model.Amount;
            decimal p = Model.Deposit;

            decimal fv;
            if (r == 0m)
            {
                // No interest: FV = PV + P * n
                fv = pv + p * n;
            }
            else
            {
                // FV = (PV * ((1 + r) ** n)) + (P * (((1 + r) ** n - 1) / r))
                var pow = (decimal)Math.Pow((double)(1 + r), n);
                fv = pv * pow + p * ((pow - 1m) / r);
            }

            ResultText = $"Future value: {fv:C}";
        }

        public void Dispose()
        {
            if (EditContext != null)
                EditContext.OnFieldChanged -= HandleFieldChanged;
        }
    }

    public class LoanModel
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Deposit must be non-negative")]
        public decimal Deposit { get; set; }

        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        public decimal AnnualInterestRate { get; set; }

        [Required]
        [Range(1, 1200, ErrorMessage = "Period must be at least 1 month")]
        public int Period { get; set; }
    }
}
