using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ khoản thu / hóa đơn (FR4.1, FR2.6 — Ngày 11 Bước 4).
/// Admin: CRUD loại phí, tạo hóa đơn 1 HS / cả lớp.
/// Parent/Student: xem hóa đơn của mình / con, lấy biên lai khi đã Paid.
/// </summary>
public interface IFeeService
{
    // ─── FeeCategory ────────────────────────────────────────────────────────

    /// <summary>DS loại phí (Admin xem tất cả; có thể lọc IsActive).</summary>
    Task<List<FeeCategoryDto>> GetCategoriesAsync(bool? isActive = null);

    /// <summary>Chi tiết loại phí.</summary>
    Task<FeeCategoryDto?> GetCategoryByIdAsync(int id);

    /// <summary>Tạo loại phí mới.</summary>
    Task<(FeeCategoryDto? Result, string? Error)> CreateCategoryAsync(CreateUpdateFeeCategoryDto dto);

    /// <summary>Sửa loại phí.</summary>
    Task<(FeeCategoryDto? Result, string? Error)> UpdateCategoryAsync(int id, CreateUpdateFeeCategoryDto dto);

    /// <summary>Xóa loại phí (chặn nếu còn hóa đơn gắn vào).</summary>
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);

    // ─── FeeInvoice ─────────────────────────────────────────────────────────

    /// <summary>Admin: DS hóa đơn lọc studentId / isPaid / status.</summary>
    Task<(List<FeeInvoiceDto>? Result, string? Error)> GetInvoicesAsync(FeeInvoiceListQueryDto query);

    /// <summary>Student/Parent: hóa đơn của tôi / các con.</summary>
    Task<(List<FeeInvoiceDto>? Result, string? Error)> GetMyInvoicesAsync(int actorId, UserRole actorRole);

    /// <summary>Chi tiết 1 hóa đơn (kiểm tra quyền xem).</summary>
    Task<(FeeInvoiceDto? Result, string? Error)> GetInvoiceByIdAsync(int id, int actorId, UserRole actorRole);

    /// <summary>Admin tạo hóa đơn cho 1 HS.</summary>
    Task<(FeeInvoiceDto? Result, string? Error)> CreateInvoiceAsync(CreateFeeInvoiceDto dto);

    /// <summary>Admin gán phí cho mọi HS trong lớp.</summary>
    Task<(BatchCreateFeeInvoiceResultDto? Result, string? Error)> CreateInvoiceBatchAsync(BatchCreateFeeInvoiceDto dto);

    /// <summary>Biên lai điện tử — chỉ khi Status = Paid.</summary>
    Task<(FeeReceiptDto? Result, string? Error)> GetReceiptAsync(int invoiceId, int actorId, UserRole actorRole);
}

/// <summary>Implement IFeeService.</summary>
public class FeeService : IFeeService
{
    private readonly IFeeCategoryRepository _categoryRepository;
    private readonly IFeeInvoiceRepository _invoiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassRepository _classRepository;
    private readonly AppDbContext _context;

    public FeeService(
        IFeeCategoryRepository categoryRepository,
        IFeeInvoiceRepository invoiceRepository,
        IUserRepository userRepository,
        IClassRepository classRepository,
        AppDbContext context)
    {
        _categoryRepository = categoryRepository;
        _invoiceRepository = invoiceRepository;
        _userRepository = userRepository;
        _classRepository = classRepository;
        _context = context;
    }

    // ─── Categories ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<List<FeeCategoryDto>> GetCategoriesAsync(bool? isActive = null)
    {
        var items = await _categoryRepository.GetAllAsync(isActive);
        return items.Select(MapCategory).ToList();
    }

    /// <inheritdoc />
    public async Task<FeeCategoryDto?> GetCategoryByIdAsync(int id)
    {
        var entity = await _categoryRepository.GetByIdAsync(id);
        return entity == null ? null : MapCategory(entity);
    }

    /// <summary>Tạo loại phí — Name + DefaultAmount &gt; 0.</summary>
    public async Task<(FeeCategoryDto? Result, string? Error)> CreateCategoryAsync(CreateUpdateFeeCategoryDto dto)
    {
        var error = ValidateCategory(dto);
        if (error != null) return (null, error);

        var entity = new FeeCategory
        {
            Name = dto.Name.Trim(),
            Description = NormalizeOptional(dto.Description),
            DefaultAmount = dto.DefaultAmount,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.CreateAsync(entity);
        return (MapCategory(created), null);
    }

    /// <inheritdoc />
    public async Task<(FeeCategoryDto? Result, string? Error)> UpdateCategoryAsync(int id, CreateUpdateFeeCategoryDto dto)
    {
        var entity = await _categoryRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy loại phí.");

        var error = ValidateCategory(dto);
        if (error != null) return (null, error);

        entity.Name = dto.Name.Trim();
        entity.Description = NormalizeOptional(dto.Description);
        entity.DefaultAmount = dto.DefaultAmount;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(entity);
        return (MapCategory(entity), null);
    }

    /// <summary>Xóa loại phí — chặn nếu còn hóa đơn tham chiếu.</summary>
    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        var entity = await _categoryRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy loại phí.");

        var inUse = await _context.FeeInvoices.AnyAsync(i => i.FeeCategoryId == id);
        if (inUse)
            return (false, "Không thể xóa: loại phí đang có hóa đơn gắn vào.");

        await _categoryRepository.DeleteAsync(entity);
        return (true, "Đã xóa loại phí.");
    }

    // ─── Invoices ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<(List<FeeInvoiceDto>? Result, string? Error)> GetInvoicesAsync(FeeInvoiceListQueryDto query)
    {
        FeePaymentStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<FeePaymentStatus>(query.Status, true, out var parsed))
        {
            status = parsed;
        }

        var items = await _invoiceRepository.GetListAsync(query.StudentId, query.IsPaid, status);
        return (items.Select(MapInvoice).ToList(), null);
    }

    /// <summary>
    /// Student: hóa đơn của chính mình.
    /// Parent: hóa đơn tất cả con liên kết StudentParent.
    /// </summary>
    public async Task<(List<FeeInvoiceDto>? Result, string? Error)> GetMyInvoicesAsync(int actorId, UserRole actorRole)
    {
        List<int> studentIds;

        if (actorRole == UserRole.Student)
        {
            studentIds = new List<int> { actorId };
        }
        else if (actorRole == UserRole.Parent)
        {
            studentIds = await _context.StudentParents
                .Where(sp => sp.ParentId == actorId)
                .Select(sp => sp.StudentId)
                .ToListAsync();

            if (studentIds.Count == 0)
                return (new List<FeeInvoiceDto>(), null);
        }
        else
        {
            return (null, "Chỉ Student/Parent dùng endpoint này.");
        }

        var items = await _invoiceRepository.GetByStudentIdsAsync(studentIds);
        return (items.Select(MapInvoice).ToList(), null);
    }

    /// <inheritdoc />
    public async Task<(FeeInvoiceDto? Result, string? Error)> GetInvoiceByIdAsync(
        int id, int actorId, UserRole actorRole)
    {
        var entity = await _invoiceRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy hóa đơn.");

        var accessError = await VerifyCanViewInvoiceAsync(entity, actorId, actorRole);
        if (accessError != null) return (null, accessError);

        return (MapInvoice(entity), null);
    }

    /// <summary>
    /// Tạo hóa đơn 1 HS — Student phải role Student;
    /// Amount null → lấy DefaultAmount của FeeCategory.
    /// </summary>
    public async Task<(FeeInvoiceDto? Result, string? Error)> CreateInvoiceAsync(CreateFeeInvoiceDto dto)
    {
        var (student, category, amount, error) = await ResolveInvoiceInputsAsync(
            dto.StudentId, dto.FeeCategoryId, dto.Amount);
        if (error != null) return (null, error);

        var entity = new FeeInvoice
        {
            StudentId = student!.Id,
            FeeCategoryId = category!.Id,
            Amount = amount,
            DueDate = dto.DueDate.ToUniversalTime(),
            Status = FeePaymentStatus.Pending,
            IsPaid = false,
            Note = NormalizeOptional(dto.Note),
            CreatedAt = DateTime.UtcNow
        };

        var created = await _invoiceRepository.CreateAsync(entity);
        var full = await _invoiceRepository.GetByIdAsync(created.Id);
        return (MapInvoice(full!), null);
    }

    /// <summary>
    /// Gán phí cả lớp: lấy mọi HS trong ClassStudents → tạo 1 hóa đơn / HS.
    /// </summary>
    public async Task<(BatchCreateFeeInvoiceResultDto? Result, string? Error)> CreateInvoiceBatchAsync(
        BatchCreateFeeInvoiceDto dto)
    {
        var cls = await _classRepository.GetByIdAsync(dto.ClassId);
        if (cls == null) return (null, "Không tìm thấy lớp học.");

        var category = await _categoryRepository.GetByIdAsync(dto.FeeCategoryId);
        if (category == null) return (null, "Không tìm thấy loại phí.");
        if (!category.IsActive) return (null, "Loại phí đang bị tắt.");

        var amount = dto.Amount ?? category.DefaultAmount;
        if (amount <= 0) return (null, "Số tiền phải lớn hơn 0.");

        var studentIds = await _context.ClassStudents
            .Where(cs => cs.ClassId == dto.ClassId)
            .Select(cs => cs.StudentId)
            .ToListAsync();

        if (studentIds.Count == 0)
            return (null, "Lớp chưa có học sinh.");

        var due = dto.DueDate.ToUniversalTime();
        var note = NormalizeOptional(dto.Note);
        var now = DateTime.UtcNow;

        var entities = studentIds.Select(sid => new FeeInvoice
        {
            StudentId = sid,
            FeeCategoryId = category.Id,
            Amount = amount,
            DueDate = due,
            Status = FeePaymentStatus.Pending,
            IsPaid = false,
            Note = note,
            CreatedAt = now
        }).ToList();

        var created = await _invoiceRepository.CreateManyAsync(entities);

        // Reload kèm navigation để map DTO
        var ids = created.Select(c => c.Id).ToList();
        var loaded = await _context.FeeInvoices
            .Include(i => i.Student)
            .Include(i => i.FeeCategory)
            .Where(i => ids.Contains(i.Id))
            .ToListAsync();

        return (new BatchCreateFeeInvoiceResultDto
        {
            CreatedCount = loaded.Count,
            Message = $"Đã tạo {loaded.Count} hóa đơn cho lớp {cls.Name}.",
            Invoices = loaded.Select(MapInvoice).ToList()
        }, null);
    }

    /// <summary>Biên lai — chỉ khi đã Paid; kiểm tra quyền xem hóa đơn.</summary>
    public async Task<(FeeReceiptDto? Result, string? Error)> GetReceiptAsync(
        int invoiceId, int actorId, UserRole actorRole)
    {
        var entity = await _invoiceRepository.GetByIdAsync(invoiceId);
        if (entity == null) return (null, "Không tìm thấy hóa đơn.");

        var accessError = await VerifyCanViewInvoiceAsync(entity, actorId, actorRole);
        if (accessError != null) return (null, accessError);

        if (entity.Status != FeePaymentStatus.Paid || !entity.IsPaid || entity.PaidAt == null)
            return (null, "Hóa đơn chưa thanh toán — chưa có biên lai.");

        return (new FeeReceiptDto
        {
            ReceiptNumber = entity.ReceiptNumber ?? $"TMP-{entity.Id}",
            InvoiceId = entity.Id,
            StudentName = entity.Student.FullName,
            FeeCategoryName = entity.FeeCategory.Name,
            Amount = entity.Amount,
            PaymentMethod = entity.PaymentMethod ?? "",
            TransactionId = entity.TransactionId,
            PaidAt = entity.PaidAt.Value
        }, null);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<(User? Student, FeeCategory? Category, decimal Amount, string? Error)>
        ResolveInvoiceInputsAsync(int studentId, int feeCategoryId, decimal? amountOverride)
    {
        var student = await _userRepository.GetUserByIdAsync(studentId);
        if (student == null) return (null, null, 0, "Không tìm thấy học sinh.");
        if (student.Role != UserRole.Student)
            return (null, null, 0, "User này không phải học sinh.");

        var category = await _categoryRepository.GetByIdAsync(feeCategoryId);
        if (category == null) return (null, null, 0, "Không tìm thấy loại phí.");
        if (!category.IsActive) return (null, null, 0, "Loại phí đang bị tắt.");

        var amount = amountOverride ?? category.DefaultAmount;
        if (amount <= 0) return (null, null, 0, "Số tiền phải lớn hơn 0.");

        return (student, category, amount, null);
    }

    /// <summary>Admin xem mọi hóa đơn; Student/Parent chỉ xem của mình / con.</summary>
    private async Task<string?> VerifyCanViewInvoiceAsync(FeeInvoice invoice, int actorId, UserRole role)
    {
        if (role == UserRole.Admin) return null;

        if (role == UserRole.Student && invoice.StudentId == actorId)
            return null;

        if (role == UserRole.Parent)
        {
            var isChild = await _context.StudentParents
                .AnyAsync(sp => sp.ParentId == actorId && sp.StudentId == invoice.StudentId);
            if (isChild) return null;
        }

        return "Bạn không có quyền xem hóa đơn này.";
    }

    private static string? ValidateCategory(CreateUpdateFeeCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Tên loại phí không được để trống.";
        if (dto.DefaultAmount <= 0)
            return "DefaultAmount phải lớn hơn 0.";
        return null;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static FeeCategoryDto MapCategory(FeeCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        DefaultAmount = c.DefaultAmount,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static FeeInvoiceDto MapInvoice(FeeInvoice i) => new()
    {
        Id = i.Id,
        StudentId = i.StudentId,
        StudentName = i.Student?.FullName ?? string.Empty,
        StudentPhone = i.Student?.Phone ?? string.Empty,
        FeeCategoryId = i.FeeCategoryId,
        FeeCategoryName = i.FeeCategory?.Name ?? string.Empty,
        Amount = i.Amount,
        DueDate = i.DueDate,
        Status = i.Status.ToString(),
        IsPaid = i.IsPaid,
        PaidAt = i.PaidAt,
        PaymentMethod = i.PaymentMethod,
        TransactionId = i.TransactionId,
        ReceiptNumber = i.ReceiptNumber,
        Note = i.Note,
        CreatedAt = i.CreatedAt
    };
}
