namespace Api.DTOs;

/// <summary>DTO trả về thông tin lớp học.</summary>
public class ClassDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    /// <summary>Số học sinh đang trong lớp.</summary>
    public int StudentCount { get; set; }
    /// <summary>GV chủ nhiệm (null = chưa gán).</summary>
    public int? HomeroomTeacherId { get; set; }
    public string? HomeroomTeacherName { get; set; }
}

/// <summary>Body tạo / cập nhật lớp (gắn với 1 kỳ học + tùy chọn GV chủ nhiệm).</summary>
public class CreateUpdateClassDto
{
    public string Name { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    /// <summary>GV chủ nhiệm — Role phải là Teacher; null = bỏ gán.</summary>
    public int? HomeroomTeacherId { get; set; }
}

/// <summary>Body thêm học sinh vào lớp.</summary>
public class AddStudentToClassDto
{
    public int StudentId { get; set; }
}

/// <summary>Học sinh trong lớp (tóm tắt).</summary>
public class ClassStudentDto
{
    public int StudentId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
