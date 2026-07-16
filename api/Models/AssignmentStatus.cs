namespace Api.Models;

/// <summary>
/// Trạng thái bài tập phía học sinh (tính runtime, không lưu DB).
/// ToDo = chưa nộp + còn hạn; Done = đã nộp; Overdue = chưa nộp + quá hạn.
/// </summary>
public static class AssignmentStatus
{
    /// <summary>Chưa nộp và còn trong hạn.</summary>
    public const string ToDo = "ToDo";

    /// <summary>Đã có bài nộp.</summary>
    public const string Done = "Done";

    /// <summary>Chưa nộp và đã quá DueDate.</summary>
    public const string Overdue = "Overdue";
}
