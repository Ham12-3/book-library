using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManager.Data;
using LibraryManager.Models;
using LibraryManager.Models.ViewModels;

namespace LibraryManager
{
    public class LoansController : Controller
    {
        private readonly LibraryContext _context;

        public LoansController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var libraryContext = _context.Loans.Include(l => l.Book).Include(l => l.Member);
            return View(await libraryContext.ToListAsync());
        }

        // GET: Loans/Borrow/5
        public async Task<IActionResult> Borrow(int bookId)
        {
            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null)
            {
                return NotFound();
            }

            if (book.AvailableCopies <= 0)
            {
                TempData["ErrorMessage"] = "This book currently has no available copies.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            var viewModel = new BorrowViewModel
            {
                BookId = book.Id,
                BookTitle = book.Title,
                AvailableCopies = book.AvailableCopies,
                BorrowedOn = DateTime.Today,
                DueOn = DateTime.Today.AddDays(14),
                MemberOptions = await BuildMemberOptionsAsync()
            };

            return View(viewModel);
        }

        // POST: Loans/Borrow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(BorrowViewModel model)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == model.BookId);
            if (book == null)
            {
                return NotFound();
            }

            if (book.AvailableCopies <= 0)
            {
                ModelState.AddModelError(string.Empty, "This book currently has no available copies.");
            }

            if (model.DueOn <= model.BorrowedOn)
            {
                ModelState.AddModelError(nameof(model.DueOn), "Due date must be after the borrowed date.");
            }

            if (!ModelState.IsValid)
            {
                model.BookTitle = book.Title;
                model.AvailableCopies = book.AvailableCopies;
                model.MemberOptions = await BuildMemberOptionsAsync();
                return View(model);
            }

            var loan = new Loan
            {
                BookId = model.BookId,
                MemberId = model.MemberId,
                BorrowedOn = model.BorrowedOn,
                DueOn = model.DueOn
            };

            if (book.AvailableCopies > 0)
            {
                book.AvailableCopies -= 1;
            }

            _context.Add(loan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{book.Title}' loaned successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound();
            }

            if (loan.ReturnedOn.HasValue)
            {
                TempData["InfoMessage"] = "This loan was already returned.";
                return RedirectToAction(nameof(Index));
            }

            loan.ReturnedOn = DateTime.UtcNow;

            if (loan.Book != null)
            {
                loan.Book.AvailableCopies += 1;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Book marked as returned.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Loans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // GET: Loans/Create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books, "Id", "Isbn");
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Email");
            return View();
        }

        // POST: Loans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,BookId,MemberId,BorrowedOn,DueOn,ReturnedOn")] Loan loan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "Id", "Isbn", loan.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Email", loan.MemberId);
            return View(loan);
        }

        // GET: Loans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Books, "Id", "Isbn", loan.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Email", loan.MemberId);
            return View(loan);
        }

        // POST: Loans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BookId,MemberId,BorrowedOn,DueOn,ReturnedOn")] Loan loan)
        {
            if (id != loan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanExists(loan.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "Id", "Isbn", loan.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Email", loan.MemberId);
            return View(loan);
        }

        // GET: Loans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // POST: Loans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan != null)
            {
                _context.Loans.Remove(loan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoanExists(int id)
        {
            return _context.Loans.Any(e => e.Id == id);
        }

        private async Task<List<SelectListItem>> BuildMemberOptionsAsync()
        {
            return await _context.Members
                .OrderBy(m => m.FullName)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.FullName
                })
                .ToListAsync();
        }
    }
}
