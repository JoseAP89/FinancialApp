using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FinancialApp.Components.Pages
{
    public partial class Home : ComponentBase, IDisposable
    {
        protected LoanModel Model { get; set; } = new LoanModel();
        protected EditContext EditContext { get; set; } = null!;
        protected List<AdditionalDeposit> AdditionalDeposits { get; set; } = new List<AdditionalDeposit>();
        protected decimal FutureValue { get; set; } = 0m;
        protected decimal AdditionalDepositsTotal { get; set; } = 0m;

        protected override void OnInitialized()
        {
            EditContext = new EditContext(Model);
            EditContext.OnFieldChanged += HandleFieldChanged;
            UpdateResult();
        }

        private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
        {
            // react to every field change (similar to reactive forms)
            UpdateResult();
        }

        protected void HandleValidSubmit()
        {
            UpdateResult();
        }

        protected void AddAdditionalDeposit()
        {
            AdditionalDeposits.Add(new AdditionalDeposit());
            UpdateResult();
        }

        protected void RemoveAdditionalDeposit(int index)
        {
            if (index >= 0 && index < AdditionalDeposits.Count)
            {
                AdditionalDeposits.RemoveAt(index);
                UpdateResult();
            }
        }

        private void UpdateResult()
        {
            if (Model.Amount <= 0 && Model.Period <= 0)
            {
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
            FutureValue = fv;

            AdditionalDepositsTotal = 0;
            // include any one-time additional deposits specified in the form
            if (AdditionalDeposits != null && AdditionalDeposits.Count > 0)
            {
                // recalculate including additional deposits (add their grown value to the computed fv)
                decimal additionalTotal = 0m;
                foreach (var ad in AdditionalDeposits)
                {
                    if (ad == null) continue;
                    // only consider deposits that occur within the total period
                    if (ad.Period <= 0 || ad.Period > n) continue;
                    int monthsToGrow = n - ad.Period;
                    var grow = (decimal)Math.Pow((double)(1 + r), monthsToGrow);
                    additionalTotal += ad.Amount * grow;
                }
                AdditionalDepositsTotal = additionalTotal;
            }
        }

        public void Dispose()
        {
            if (EditContext != null)
                EditContext.OnFieldChanged -= HandleFieldChanged;
        }
    }

    public class LoanModel
    {
        public decimal Amount { get; set; } = 0m;

        [Range(0, double.MaxValue, ErrorMessage = "Deposit must be non-negative")]
        public decimal Deposit { get; set; }

        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        public decimal AnnualInterestRate { get; set; }

        [Required]
        [Range(1, 1200, ErrorMessage = "Period must be at least 1 month")]
        public int Period { get; set; }
    }

    public class AdditionalDeposit
    {
        public decimal Amount { get; set; } = 0m;
        public int Period { get; set; } = 0; // month at which this one-time deposit is made
    }
}
