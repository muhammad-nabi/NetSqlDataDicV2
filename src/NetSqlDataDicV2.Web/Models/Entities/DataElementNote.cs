namespace NetSqlDataDicV2.Web.Models.Entities;

public class DataElementNote
{
    public int DataElementNoteId { get; set; }

    // Foreign Key
    public int DataElementId { get; set; }
    public DataElement DataElement { get; set; } = null!;

    // Note content
    public string NoteText { get; set; } = string.Empty;

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
