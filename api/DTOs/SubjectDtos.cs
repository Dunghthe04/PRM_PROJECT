namespace Api.DTOs;

/// <summary>DTO trả về thông tin môn học.</summary>
public class SubjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>Body tạo / cập nhật môn học (Toán, Văn, ...).</summary>
public class CreateUpdateSubjectDto
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Mã môn duy nhất (vd: MATH, LIT, ENG).</summary>
    public string Code { get; set; } = string.Empty;
}
