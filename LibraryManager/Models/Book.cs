using System.ComponentModel.DataAnnotations;

namespace LibraryManager.Models;

public class Book
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(32)]
    public string Isbn { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? PublishedOn { get; set; }

    [Range(1, 500)]
    public int TotalCopies { get; set; } = 1;

    public int AvailableCopies { get; set; } = 1;

    [StringLength(260)]
    public string? CoverImagePath { get; set; }

    [Required]
    public int AuthorId { get; set; }

    public Author? Author { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
