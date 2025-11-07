using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManager.Models.ViewModels;

public class BorrowViewModel
{
    [Required]
    public int BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public int AvailableCopies { get; set; }

    [Display(Name = "Member")]
    [Required(ErrorMessage = "Select a member to borrow this book.")]
    public int MemberId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Borrowed On")]
    public DateTime BorrowedOn { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Due On")]
    public DateTime DueOn { get; set; } = DateTime.Today.AddDays(14);

    public IEnumerable<SelectListItem> MemberOptions { get; set; } = new List<SelectListItem>();
}
