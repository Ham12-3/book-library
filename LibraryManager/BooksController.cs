using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManager.Data;
using LibraryManager.Models;

namespace LibraryManager
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly IWebHostEnvironment _environment;

        public BooksController(LibraryContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Books
        public async Task<IActionResult> Index(string? query, int? authorId, bool? availableOnly)
        {
            var booksQuery = _context.Books
                .Include(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                var likeTerm = $"%{term}%";
                booksQuery = booksQuery.Where(b =>
                    EF.Functions.Like(b.Title, likeTerm) ||
                    EF.Functions.Like(b.Isbn, likeTerm));
            }

            if (authorId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.AuthorId == authorId.Value);
            }

            if (availableOnly.GetValueOrDefault())
            {
                booksQuery = booksQuery.Where(b => b.AvailableCopies > 0);
            }

            booksQuery = booksQuery.OrderBy(b => b.Title);

            var authors = await _context.Authors
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .AsNoTracking()
                .ToListAsync();

            ViewData["Authors"] = new SelectList(authors, nameof(Author.Id), nameof(Author.FullName), authorId);
            ViewData["Query"] = query;
            ViewData["AvailableOnly"] = availableOnly ?? false;

            var books = await booksQuery.AsNoTracking().ToListAsync();
            return View(books);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "FirstName");
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Isbn,PublishedOn,TotalCopies,AvailableCopies,AuthorId,CoverImagePath")] Book book, IFormFile? coverImage)
        {
            if (ModelState.IsValid)
            {
                if (coverImage != null)
                {
                    book.CoverImagePath = await SaveCoverImageAsync(coverImage);
                }

                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "FirstName", book.AuthorId);
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "FirstName", book.AuthorId);
            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Isbn,PublishedOn,TotalCopies,AvailableCopies,AuthorId,CoverImagePath")] Book book, IFormFile? coverImage, bool removeImage = false)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBook == null)
                    {
                        return NotFound();
                    }

                    if (removeImage)
                    {
                        DeleteCoverImage(existingBook.CoverImagePath);
                        book.CoverImagePath = null;
                    }
                    else if (coverImage != null)
                    {
                        DeleteCoverImage(existingBook.CoverImagePath);
                        book.CoverImagePath = await SaveCoverImageAsync(coverImage);
                    }
                    else
                    {
                        book.CoverImagePath = existingBook.CoverImagePath;
                    }

                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
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
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "FirstName", book.AuthorId);
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                DeleteCoverImage(book.CoverImagePath);
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }

        private async Task<string?> SaveCoverImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "covers");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/covers/{fileName}";
        }

        private void DeleteCoverImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var sanitized = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, sanitized);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
