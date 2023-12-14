namespace CloudFiles.Database.Models;

public class UserFile
{
    public int Id { get; set; }
    
    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public long Bytes { get; set; }
    
    public Guid Guid { get; set; }

    public string Extenstion { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}