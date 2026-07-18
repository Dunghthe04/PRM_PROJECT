namespace Api.DTOs;

/// <summary>DTO trả về thông tin kỳ học.</summary>
public class SemesterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>Body tạo / cập nhật kỳ học (HK1, HK2, ...).</summary>
public class CreateUpdateSemesterDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
