namespace Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // Relationship: A Parent can have many Children, a Child can have many Parents
    public ICollection<StudentParent> ParentLinks { get; set; } = new List<StudentParent>();
    public ICollection<StudentParent> ChildLinks { get; set; } = new List<StudentParent>();
}

public class StudentParent
{
    public int ParentId { get; set; }
    public User Parent { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}
