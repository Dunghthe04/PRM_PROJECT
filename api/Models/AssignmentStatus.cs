namespace Api.Models;

/// <summary>
/// Trạng thái bài tập phía học sinh (tính runtime, không lưu DB).
/// ToDo = chưa nộp + còn hạn; Done = đã nộp; Overdue = chưa nộp + quá hạn.
/// </summary>
public static class AssignmentStatus
{
    public const string ToDo = "ToDo";
    public const string Done = "Done";
    public const string Overdue = "Overdue";
}
