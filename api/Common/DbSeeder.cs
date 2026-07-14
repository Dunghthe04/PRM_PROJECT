using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Common;

/// <summary>
/// Seed dữ liệu mẫu cho môi trường Development (idempotent — chỉ tạo khi thiếu,
/// chạy lại nhiều lần không nhân đôi). Giúp test nhanh Ngày 14:
/// 5 tài khoản vai trò, quan hệ phụ huynh–con (Switch Profile), bảng tin, thông báo.
///
/// Mọi tài khoản mẫu dùng chung mật khẩu: 123456
/// </summary>
public static class DbSeeder
{
    private const string DemoPassword = "123456";

    /// <summary>
    /// Chạy seed. Nhận: [db] DbContext. Trả về: Task.
    /// Thứ tự: user → liên kết phụ huynh-con → bảng tin → thông báo.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1) Tài khoản mẫu — tạo nếu chưa có (khớp theo SĐT).
        var admin = await EnsureUserAsync(db, "0900000000", "Admin Demo", UserRole.Admin);
        var teacher = await EnsureUserAsync(db, "0900000002", "Giao Vien Demo", UserRole.Teacher);
        var parent = await EnsureUserAsync(db, "0900000003", "Phu Huynh Demo", UserRole.Parent);
        var student1 = await EnsureUserAsync(db, "0900000004", "Hoc Sinh A", UserRole.Student);
        var student2 = await EnsureUserAsync(db, "0900000005", "Hoc Sinh B", UserRole.Student);
        await db.SaveChangesAsync(); // lưu để các user có Id

        // 2) Gán con cho phụ huynh (phục vụ Switch Profile - FR2.1).
        await EnsureLinkAsync(db, parent.Id, student1.Id);
        await EnsureLinkAsync(db, parent.Id, student2.Id);
        await db.SaveChangesAsync();

        // 3) Bảng tin mẫu — chỉ seed khi CHƯA có bảng tin nào.
        if (!await db.Announcements.AnyAsync())
        {
            db.Announcements.AddRange(
                new Announcement
                {
                    Title = "Chào mừng năm học mới 2026-2027",
                    Content = "Nhà trường kính chúc quý phụ huynh và các em học sinh một năm học nhiều thành công.",
                    Type = AnnouncementType.Global,
                    CreatedById = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                },
                new Announcement
                {
                    Title = "Lịch họp phụ huynh đầu năm",
                    Content = "Buổi họp phụ huynh sẽ diễn ra vào Chủ nhật tuần này lúc 8h00 tại hội trường.",
                    Type = AnnouncementType.Global,
                    CreatedById = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                },
                new Announcement
                {
                    Title = "Thông báo nghỉ lễ Quốc khánh 2/9",
                    Content = "Học sinh được nghỉ ngày 2/9. Lịch học trở lại bình thường vào ngày 3/9.",
                    Type = AnnouncementType.Global,
                    CreatedById = admin.Id,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync();
        }

        // 4) Thông báo cá nhân mẫu — chỉ seed khi CHƯA có thông báo nào.
        if (!await db.Notifications.AnyAsync())
        {
            db.Notifications.AddRange(
                new Notification
                {
                    UserId = parent.Id,
                    Title = "Điểm mới",
                    Message = "Con của bạn vừa có điểm kiểm tra môn Toán.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                },
                new Notification
                {
                    UserId = parent.Id,
                    Title = "Nhắc học phí",
                    Message = "Học phí tháng này sắp đến hạn thanh toán.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                },
                new Notification
                {
                    UserId = student1.Id,
                    Title = "Bài tập mới",
                    Message = "Bạn có bài tập môn Ngữ Văn cần hoàn thành.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                }
            );
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Tạo user nếu SĐT chưa tồn tại; trả về user (cũ hoặc mới).</summary>
    private static async Task<User> EnsureUserAsync(
        AppDbContext db, string phone, string fullName, UserRole role)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
        if (user != null) return user;

        user = new User
        {
            Phone = phone,
            Username = phone, // Username = Phone theo quy ước dự án
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
            Role = role,
            IsPhoneVerified = true, // để đăng nhập được ngay
            IsLocked = false,
        };
        db.Users.Add(user);
        return user;
    }

    /// <summary>Thêm liên kết phụ huynh–con nếu chưa có.</summary>
    private static async Task EnsureLinkAsync(AppDbContext db, int parentId, int studentId)
    {
        var exists = await db.StudentParents
            .AnyAsync(sp => sp.ParentId == parentId && sp.StudentId == studentId);
        if (!exists)
            db.StudentParents.Add(new StudentParent { ParentId = parentId, StudentId = studentId });
    }
}
