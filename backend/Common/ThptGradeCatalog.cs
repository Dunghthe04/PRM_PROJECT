namespace Api.Common;

/// <summary>
/// Cột điểm sổ điện tử THPT (thực tế nhà trường / Sở GD vẫn dùng trên sổ điểm):
/// 3 miệng · 3 lần 15 phút · 2 lần 1 tiết · 1 giữa kỳ · 1 cuối kỳ.
/// TBHK = Σ(điểm × hệ số) / Σ hệ số (ô trống không tính).
/// </summary>
public static class ThptGradeCatalog
{
    public sealed record Column(string Code, string HeaderVi, int Weight);

    public static readonly IReadOnlyList<Column> Columns = new[]
    {
        new Column("Oral1", "Miệng 1", 1),
        new Column("Oral2", "Miệng 2", 1),
        new Column("Oral3", "Miệng 3", 1),
        new Column("Quiz15_1", "15 phút 1", 1),
        new Column("Quiz15_2", "15 phút 2", 1),
        new Column("Quiz15_3", "15 phút 3", 1),
        new Column("OnePeriod1", "1 tiết 1", 2),
        new Column("OnePeriod2", "1 tiết 2", 2),
        new Column("Midterm", "Giữa kỳ", 2),
        new Column("Final", "Cuối kỳ", 3),
    };

    /// <summary>Map header Excel (đã normalize) → mã AssessmentType.</summary>
    public static string? MapHeaderToCode(string normalized)
    {
        foreach (var c in Columns)
        {
            if (Normalize(c.HeaderVi) == normalized) return c.Code;
            if (Normalize(c.Code) == normalized) return c.Code;
        }

        // Alias cũ / viết tắt
        return normalized switch
        {
            "miệng" or "mieng" or "oral" => "Oral1",
            "15 phút" or "15 phut" or "quiz15" => "Quiz15_1",
            "1 tiết" or "1 tiet" or "oneperiod" => "OnePeriod1",
            "giữa kỳ" or "giua ky" or "midterm" or "đđggk" => "Midterm",
            "cuối kỳ" or "cuoi ky" or "final" or "đđgck" => "Final",
            _ => null
        };
    }

    public static string Normalize(string raw) =>
        raw.Trim().ToLowerInvariant();
}
