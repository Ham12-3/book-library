using System.ComponentModel.DataAnnotations;

namespace LibraryManager.Models;

public class Author
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = new List<Book>();

    public string FullName => $"{FirstName} {LastName}";
}