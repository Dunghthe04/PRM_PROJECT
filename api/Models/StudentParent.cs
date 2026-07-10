namespace Api.Models;

public class StudentParent
{
    public int ParentId { get; set; }
    public User Parent { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}
