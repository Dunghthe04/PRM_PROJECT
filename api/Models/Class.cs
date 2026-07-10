namespace Api.Models;

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
}
