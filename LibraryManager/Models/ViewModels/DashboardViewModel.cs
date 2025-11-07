using System.Collections.Generic;
using LibraryManager.Models;

namespace LibraryManager.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalBooks { get; set; }
    public int AvailableBooks { get; set; }
    public int TotalMembers { get; set; }
    public int ActiveLoans { get; set; }
    public List<Loan> OverdueLoans { get; set; } = new();

    public int OverdueCount => OverdueLoans?.Count ?? 0;
}
