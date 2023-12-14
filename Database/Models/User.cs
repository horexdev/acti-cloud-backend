namespace CloudFiles.Database.Models;

public class User
{
    public int Id { get; set; }
    
    public int RoleId { get; set; }
    
    public string Name { get; set; } = null!;

    public byte[] Password { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime LastLoginAt { get; set; }

    public byte[]? Avatar { get; set; }
    
    public virtual Role Role { get; set; } = null!;
}