namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class DataElementNoteViewModel
{
    public int DataElementNoteId { get; set; }
    public int DataElementId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Display properties
    public string FormattedDate => CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public string RelativeTime
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalDays > 30) return CreatedAt.ToString("MMM d, yyyy");
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays} day(s) ago";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours} hour(s) ago";
            if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes} minute(s) ago";
            return "Just now";
        }
    }
}
