using FinancialApp.Data.Models;

namespace FinancialApp.Core.DTOs;

public class TransactionLineDTO
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
    public bool PaidWithCreditCard { get; set; } = false;
    public System.Timers.Timer? ChildPopoverTimer { get; set; }
}