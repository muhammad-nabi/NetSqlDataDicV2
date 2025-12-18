using System.ComponentModel.DataAnnotations;

namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class AddNoteViewModel
{
    public int DataElementId { get; set; }

    [Required(ErrorMessage = "Note text is required")]
    [MaxLength(2000, ErrorMessage = "Note cannot exceed 2000 characters")]
    public string NoteText { get; set; } = string.Empty;
}
