using System.ComponentModel.DataAnnotations;

namespace LibraryManager.Models;

public class Loan
{
    public int Id { get; set; }

    [Required]
    public int BookId { get; set; }

    public Book? Book { get; set; }

    [Required]
    public int MemberId { get; set; }

    public Member? Member { get; set; }

    [DataType(DataType.Date)]
    public DateTime BorrowedOn { get; set; } = DateTime.UtcNow;

    [DataType(DataType.Date)]
    public DateTime DueOn { get; set; } = DateTime.UtcNow.AddDays(14);

    [DataType(DataType.Date)]
    public DateTime? ReturnedOn { get; set; }

    public bool IsReturned => ReturnedOn.HasValue;
}