namespace Api.Models;

public class Semester
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    // A class can have many students
    public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
}

public class ClassStudent
{
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}

// Represents Teacher assigned to a Class for a specific Subject
public class TeacherAssignment
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}
