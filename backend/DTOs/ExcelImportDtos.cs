namespace Api.DTOs;

/// <summary>Kết quả import Excel (users / điểm).</summary>
public class ExcelImportResultDto
{
    public int SuccessCount { get; set; }
    public int SkipCount { get; set; }
    public int ErrorCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
