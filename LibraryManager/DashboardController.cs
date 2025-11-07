using System;
using System.Linq;
using System.Threading.Tasks;
using LibraryManager.Data;
using LibraryManager.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManager;

public class DashboardController : Controller
{
    private readonly LibraryContext _context;

    public DashboardController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var overdueLoans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .Where(l => l.ReturnedOn == null && l.DueOn < DateTime.Today)
            .OrderBy(l => l.DueOn)
            .AsNoTracking()
            .ToListAsync();

        var totalBooks = await _context.Books.CountAsync();
        var availableBooks = await _context.Books.SumAsync(b => (int?)b.AvailableCopies) ?? 0;
        var totalMembers = await _context.Members.CountAsync();
        var activeLoans = await _context.Loans.CountAsync(l => l.ReturnedOn == null);

        var viewModel = new DashboardViewModel
        {
            TotalBooks = totalBooks,
            AvailableBooks = availableBooks,
            TotalMembers = totalMembers,
            ActiveLoans = activeLoans,
            OverdueLoans = overdueLoans
        };

        return View(viewModel);
    }
}
