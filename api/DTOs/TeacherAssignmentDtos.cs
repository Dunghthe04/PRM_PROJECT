namespace Api.DTOs;

/// <summary>DTO trả về 1 phân công giảng dạy.</summary>
public class TeacherAssignmentDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherPhone { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
}

/// <summary>Body tạo / cập nhật phân công: GV dạy môn X tại lớp Y.</summary>
public class CreateUpdateTeacherAssignmentDto
{
    public int TeacherId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
}

/// <summary>Lớp mà một GV đang phụ trách (kèm môn).</summary>
public class TeacherClassDto
{
    public int AssignmentId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
}
