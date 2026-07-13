using System.Text.Json;
using Api.Common;
using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ thanh toán VNPay / PayOS + cấu hình cổng (FR4.2, FR2.6 — Ngày 11 Bước 5).
/// Parent tạo link; webhook/IPN verify chữ ký → đánh dấu Paid + biên lai.
/// Không lưu số thẻ / CVV (NFR4.3).
/// </summary>
public interface IPaymentService
{
    /// <summary>Parent tạo URL thanh toán VNPay cho 1 hóa đơn Pending.</summary>
    Task<(CreatePaymentResultDto? Result, string? Error)> CreateVnPayAsync(
        CreatePaymentDto dto, int actorId, UserRole actorRole);

    /// <summary>Parent tạo link PayOS cho 1 hóa đơn Pending.</summary>
    Task<(CreatePaymentResultDto? Result, string? Error)> CreatePayOsAsync(
        CreatePaymentDto dto, int actorId, UserRole actorRole);

    /// <summary>Return URL VNPay (trình duyệt) — chỉ báo trạng thái; IPN mới là nguồn tin cậy.</summary>
    Task<(object Result, int StatusCode)> HandleVnPayReturnAsync(IDictionary<string, string> query);

    /// <summary>IPN/webhook VNPay — verify chữ ký + cập nhật Paid.</summary>
    Task<object> HandleVnPayIpnAsync(IDictionary<string, string> query);

    /// <summary>Webhook PayOS — verify checksum + cập nhật Paid.</summary>
    Task<(bool Success, string Message)> HandlePayOsWebhookAsync(JsonElement body);

    /// <summary>
    /// Dev-only: giả lập cổng báo Paid (khi chưa có sandbox thật).
    /// Chỉ dùng trong Development.
    /// </summary>
    Task<(PaymentTransactionDto? Result, string? Error)> SimulatePaidAsync(string orderCode);

    /// <summary>Lịch sử giao dịch theo studentId (Parent/Admin).</summary>
    Task<(List<PaymentTransactionDto>? Result, string? Error)> GetHistoryAsync(
        int? studentId, int actorId, UserRole actorRole);

    /// <summary>Admin xem cấu hình cổng (mask secret trong ConfigJson).</summary>
    Task<List<PaymentGatewayConfigDto>> GetConfigsAsync();

    /// <summary>Admin upsert API key VNPay/PayOS.</summary>
    Task<(PaymentGatewayConfigDto? Result, string? Error)> UpsertConfigAsync(UpdatePaymentGatewayConfigDto dto);
}

/// <summary>Implement IPaymentService.</summary>
public class PaymentService : IPaymentService
{
    private const string ProviderVnPay = "VNPay";
    private const string ProviderPayOs = "PayOS";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IFeeInvoiceRepository _invoiceRepository;
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IFeeInvoiceRepository invoiceRepository,
        AppDbContext context,
        INotificationService notificationService,
        IConfiguration configuration,
        IHostEnvironment env,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _context = context;
        _notificationService = notificationService;
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(CreatePaymentResultDto? Result, string? Error)> CreateVnPayAsync(
        CreatePaymentDto dto, int actorId, UserRole actorRole)
        => await CreatePaymentInternalAsync(dto, actorId, actorRole, ProviderVnPay);

    /// <inheritdoc />
    public async Task<(CreatePaymentResultDto? Result, string? Error)> CreatePayOsAsync(
        CreatePaymentDto dto, int actorId, UserRole actorRole)
        => await CreatePaymentInternalAsync(dto, actorId, actorRole, ProviderPayOs);

    /// <inheritdoc />
    public async Task<(object Result, int StatusCode)> HandleVnPayReturnAsync(IDictionary<string, string> query)
    {
        var (ok, message, _) = await ProcessVnPayCallbackAsync(query, isIpn: false);
        return (new { success = ok, message }, ok ? 200 : 400);
    }

    /// <inheritdoc />
    public async Task<object> HandleVnPayIpnAsync(IDictionary<string, string> query)
    {
        var (ok, message, _) = await ProcessVnPayCallbackAsync(query, isIpn: true);
        // VNPay yêu cầu RspCode dạng này
        return new
        {
            RspCode = ok ? "00" : "97",
            Message = message
        };
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> HandlePayOsWebhookAsync(JsonElement body)
    {
        var config = await _paymentRepository.GetConfigByProviderAsync(ProviderPayOs);
        if (config == null || !config.IsEnabled)
            return (false, "PayOS chưa được cấu hình.");

        var cfg = ParseConfig(config.ConfigJson);
        if (!cfg.TryGetValue("ChecksumKey", out var checksumKey) || string.IsNullOrWhiteSpace(checksumKey))
            return (false, "Thiếu ChecksumKey PayOS.");

        if (!body.TryGetProperty("data", out var dataEl))
            return (false, "Webhook thiếu field data.");

        var signature = body.TryGetProperty("signature", out var sigEl)
            ? sigEl.GetString() ?? ""
            : "";

        // Dev stub: cho phép bỏ qua verify nếu ChecksumKey = "DEV"
        var skipVerify = _env.IsDevelopment()
            && string.Equals(checksumKey, "DEV", StringComparison.OrdinalIgnoreCase);

        if (!skipVerify && !PayOsHelper.ValidateWebhookSignature(dataEl, signature, checksumKey))
            return (false, "Checksum PayOS không hợp lệ.");

        // orderCode PayOS thường là số — thử khớp OrderCode nội bộ
        if (!dataEl.TryGetProperty("orderCode", out var orderEl))
            return (false, "Thiếu orderCode.");

        var rawOrder = orderEl.ValueKind == JsonValueKind.Number
            ? orderEl.GetInt64().ToString()
            : orderEl.GetString() ?? "";

        if (string.IsNullOrWhiteSpace(rawOrder))
            return (false, "Thiếu orderCode.");

        var txn = await _paymentRepository.GetTransactionByOrderCodeAsync(rawOrder)
            ?? await _paymentRepository.GetTransactionByOrderCodeAsync($"PAYOS-{rawOrder}")
            ?? await FindTxnByPayOsNumericAsync(rawOrder);

        if (txn == null) return (false, "Không tìm thấy giao dịch.");
        var code = dataEl.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
        var status = dataEl.TryGetProperty("desc", out var descEl) ? descEl.GetString() : null;
        var paid = string.Equals(code, "00", StringComparison.OrdinalIgnoreCase)
            || (status != null && status.Contains("success", StringComparison.OrdinalIgnoreCase))
            || (dataEl.TryGetProperty("status", out var st) &&
                string.Equals(st.GetString(), "PAID", StringComparison.OrdinalIgnoreCase));

        txn.RawCallback = body.GetRawText();
        txn.UpdatedAt = DateTime.UtcNow;

        if (dataEl.TryGetProperty("paymentLinkId", out var pl))
            txn.ProviderTransactionId = pl.ToString();
        else if (dataEl.TryGetProperty("reference", out var rf))
            txn.ProviderTransactionId = rf.ToString();

        if (paid)
        {
            await MarkPaidAsync(txn, ProviderPayOs);
            return (true, "Đã xác nhận thanh toán PayOS.");
        }

        txn.Status = FeePaymentStatus.Failed;
        await _paymentRepository.UpdateTransactionAsync(txn);
        return (true, "Giao dịch PayOS không thành công — đã ghi Failed.");
    }

    /// <inheritdoc />
    public async Task<(PaymentTransactionDto? Result, string? Error)> SimulatePaidAsync(string orderCode)
    {
        if (!_env.IsDevelopment())
            return (null, "Chỉ dùng được ở môi trường Development.");

        if (string.IsNullOrWhiteSpace(orderCode))
            return (null, "orderCode là bắt buộc.");

        var txn = await _paymentRepository.GetTransactionByOrderCodeAsync(orderCode.Trim());
        if (txn == null) return (null, "Không tìm thấy giao dịch.");

        await MarkPaidAsync(txn, txn.Provider);
        txn = await _paymentRepository.GetTransactionByOrderCodeAsync(orderCode.Trim());
        return (MapTxn(txn!), null);
    }

    /// <inheritdoc />
    public async Task<(List<PaymentTransactionDto>? Result, string? Error)> GetHistoryAsync(
        int? studentId, int actorId, UserRole actorRole)
    {
        List<int> invoiceIds;

        if (actorRole == UserRole.Admin)
        {
            if (studentId.HasValue)
            {
                var invoices = await _invoiceRepository.GetListAsync(studentId, null, null);
                invoiceIds = invoices.Select(i => i.Id).ToList();
            }
            else
            {
                var all = await _invoiceRepository.GetListAsync(null, null, null);
                invoiceIds = all.Select(i => i.Id).ToList();
            }
        }
        else if (actorRole == UserRole.Parent)
        {
            var childIds = await _context.StudentParents
                .Where(sp => sp.ParentId == actorId)
                .Select(sp => sp.StudentId)
                .ToListAsync();

            if (studentId.HasValue && !childIds.Contains(studentId.Value))
                return (null, "Học sinh không thuộc phụ huynh này.");

            var targetIds = studentId.HasValue ? new List<int> { studentId.Value } : childIds;
            var invoices = await _invoiceRepository.GetByStudentIdsAsync(targetIds);
            invoiceIds = invoices.Select(i => i.Id).ToList();
        }
        else if (actorRole == UserRole.Student)
        {
            if (studentId.HasValue && studentId.Value != actorId)
                return (null, "Bạn chỉ xem được lịch sử của mình.");

            var invoices = await _invoiceRepository.GetListAsync(actorId, null, null);
            invoiceIds = invoices.Select(i => i.Id).ToList();
        }
        else
        {
            return (null, "Không có quyền xem lịch sử thanh toán.");
        }

        var txns = await _paymentRepository.GetHistoryByInvoiceIdsAsync(invoiceIds);
        return (txns.Select(MapTxn).ToList(), null);
    }

    /// <inheritdoc />
    public async Task<List<PaymentGatewayConfigDto>> GetConfigsAsync()
    {
        var configs = await _paymentRepository.GetAllConfigsAsync();
        return configs.Select(c => new PaymentGatewayConfigDto
        {
            Id = c.Id,
            Provider = c.Provider,
            IsEnabled = c.IsEnabled,
            ConfigJson = MaskSecrets(c.ConfigJson),
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<(PaymentGatewayConfigDto? Result, string? Error)> UpsertConfigAsync(
        UpdatePaymentGatewayConfigDto dto)
    {
        var provider = dto.Provider?.Trim() ?? "";
        if (!string.Equals(provider, ProviderVnPay, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(provider, ProviderPayOs, StringComparison.OrdinalIgnoreCase))
            return (null, "Provider phải là VNPay hoặc PayOS.");

        provider = string.Equals(provider, ProviderVnPay, StringComparison.OrdinalIgnoreCase)
            ? ProviderVnPay
            : ProviderPayOs;

        if (string.IsNullOrWhiteSpace(dto.ConfigJson))
            return (null, "ConfigJson không được để trống.");

        try
        {
            using var _ = JsonDocument.Parse(dto.ConfigJson);
        }
        catch (JsonException)
        {
            return (null, "ConfigJson không phải JSON hợp lệ.");
        }

        var saved = await _paymentRepository.UpsertConfigAsync(new PaymentGatewayConfig
        {
            Provider = provider,
            IsEnabled = dto.IsEnabled,
            ConfigJson = dto.ConfigJson.Trim(),
            UpdatedAt = DateTime.UtcNow
        });

        return (new PaymentGatewayConfigDto
        {
            Id = saved.Id,
            Provider = saved.Provider,
            IsEnabled = saved.IsEnabled,
            ConfigJson = MaskSecrets(saved.ConfigJson),
            UpdatedAt = saved.UpdatedAt
        }, null);
    }

    // ─── Internal ───────────────────────────────────────────────────────────

    private async Task<(CreatePaymentResultDto? Result, string? Error)> CreatePaymentInternalAsync(
        CreatePaymentDto dto, int actorId, UserRole actorRole, string provider)
    {
        if (actorRole != UserRole.Parent && actorRole != UserRole.Admin)
            return (null, "Chỉ phụ huynh (hoặc Admin) được tạo thanh toán.");

        var invoice = await _invoiceRepository.GetByIdAsync(dto.FeeInvoiceId);
        if (invoice == null) return (null, "Không tìm thấy hóa đơn.");
        if (invoice.Status != FeePaymentStatus.Pending || invoice.IsPaid)
            return (null, "Hóa đơn không ở trạng thái chờ thanh toán.");

        if (actorRole == UserRole.Parent)
        {
            var isChild = await _context.StudentParents
                .AnyAsync(sp => sp.ParentId == actorId && sp.StudentId == invoice.StudentId);
            if (!isChild) return (null, "Bạn không có quyền thanh toán hóa đơn này.");
        }

        var config = await _paymentRepository.GetConfigByProviderAsync(provider);
        if (config == null || !config.IsEnabled)
        {
            // Dev: cho phép stub khi chưa cấu hình
            if (!_env.IsDevelopment())
                return (null, $"{provider} chưa được cấu hình hoặc đang tắt.");
        }

        var orderCode = $"{provider.ToUpperInvariant()}-{invoice.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        string paymentUrl;

        if (provider == ProviderVnPay)
            paymentUrl = BuildVnPayUrl(invoice, orderCode, config);
        else
            paymentUrl = await BuildPayOsUrlAsync(invoice, orderCode, config);

        var txn = await _paymentRepository.CreateTransactionAsync(new PaymentTransaction
        {
            FeeInvoiceId = invoice.Id,
            Provider = provider,
            OrderCode = orderCode,
            Amount = invoice.Amount,
            Status = FeePaymentStatus.Pending,
            PaymentUrl = paymentUrl,
            CreatedAt = DateTime.UtcNow
        });

        return (new CreatePaymentResultDto
        {
            Provider = provider,
            OrderCode = txn.OrderCode,
            PaymentUrl = paymentUrl,
            Amount = txn.Amount
        }, null);
    }

    private string BuildVnPayUrl(
        FeeInvoice invoice, string orderCode, PaymentGatewayConfig? config)
    {
        var cfg = ParseConfig(config?.ConfigJson ?? "{}");
        var tmnCode = cfg.GetValueOrDefault("TmnCode", "DEVTMN");
        var hashSecret = cfg.GetValueOrDefault("HashSecret", "DEV");
        var paymentBase = cfg.GetValueOrDefault("PaymentUrl",
            "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");
        var publicBase = _configuration["Payment:PublicBaseUrl"]?.TrimEnd('/')
            ?? "https://localhost:7000";
        var returnUrl = cfg.GetValueOrDefault("ReturnUrl", $"{publicBase}/api/payments/vnpay/return");

        // Stub Dev: không có secret thật → URL nội bộ để simulate
        if (_env.IsDevelopment()
            && (config == null || string.Equals(hashSecret, "DEV", StringComparison.OrdinalIgnoreCase)))
        {
            return $"{publicBase}/api/payments/dev/simulate-paid?orderCode={Uri.EscapeDataString(orderCode)}";
        }

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = VnPayHelper.ToVnPayAmount(invoice.Amount).ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = orderCode,
            ["vnp_OrderInfo"] = $"Thanh toan hoa don #{invoice.Id}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_CreateDate"] = VnPayHelper.FormatCreateDate(DateTime.UtcNow)
        };

        return VnPayHelper.BuildPaymentUrl(paymentBase, parameters, hashSecret);
    }

    private async Task<string> BuildPayOsUrlAsync(
        FeeInvoice invoice, string orderCode, PaymentGatewayConfig? config)
    {
        var cfg = ParseConfig(config?.ConfigJson ?? "{}");
        var checksumKey = cfg.GetValueOrDefault("ChecksumKey", "DEV");
        var publicBase = _configuration["Payment:PublicBaseUrl"]?.TrimEnd('/')
            ?? "https://localhost:7000";
        var returnUrl = cfg.GetValueOrDefault("ReturnUrl", $"{publicBase}/swagger");
        var cancelUrl = cfg.GetValueOrDefault("CancelUrl", $"{publicBase}/swagger");

        if (_env.IsDevelopment()
            && (config == null || string.Equals(checksumKey, "DEV", StringComparison.OrdinalIgnoreCase)))
        {
            return $"{publicBase}/api/payments/dev/simulate-paid?orderCode={Uri.EscapeDataString(orderCode)}";
        }

        // PayOS thật: gọi REST API tạo payment link.
        // Khi có ClientId/ApiKey — ký và POST; thiếu thì fallback stub URL có chữ ký demo.
        var clientId = cfg.GetValueOrDefault("ClientId", "");
        var apiKey = cfg.GetValueOrDefault("ApiKey", "");
        var numericOrder = Math.Abs(orderCode.GetHashCode());
        if (numericOrder < 1_000_000) numericOrder += 1_000_000;

        var amount = PayOsHelper.ToPayOsAmount(invoice.Amount);
        var description = $"Hoa don #{invoice.Id}";
        var signature = PayOsHelper.SignCreatePayment(
            numericOrder, amount, description, cancelUrl, returnUrl, checksumKey);

        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("x-client-id", clientId);
                http.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var payload = new
                {
                    orderCode = numericOrder,
                    amount,
                    description,
                    cancelUrl,
                    returnUrl,
                    signature
                };

                var response = await http.PostAsJsonAsync(
                    "https://api-merchant.payos.vn/v2/payment-requests", payload);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("checkoutUrl", out var urlEl))
                {
                    var url = urlEl.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                        return url;
                }

                _logger.LogWarning("PayOS create link failed: {Body}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi PayOS API");
            }
        }

        // Fallback: trang checkout giả (vẫn lưu OrderCode để webhook/dev simulate)
        return $"{publicBase}/api/payments/dev/simulate-paid?orderCode={Uri.EscapeDataString(orderCode)}&sig={signature}";
    }

    private async Task<(bool Ok, string Message, PaymentTransaction? Txn)> ProcessVnPayCallbackAsync(
        IDictionary<string, string> query, bool isIpn)
    {
        var config = await _paymentRepository.GetConfigByProviderAsync(ProviderVnPay);
        var cfg = ParseConfig(config?.ConfigJson ?? "{}");
        var hashSecret = cfg.GetValueOrDefault("HashSecret", "DEV");

        var skipVerify = _env.IsDevelopment()
            && string.Equals(hashSecret, "DEV", StringComparison.OrdinalIgnoreCase);

        if (!skipVerify && !VnPayHelper.ValidateSignature(query, hashSecret))
            return (false, "Chữ ký VNPay không hợp lệ.", null);

        if (!query.TryGetValue("vnp_TxnRef", out var orderCode) || string.IsNullOrWhiteSpace(orderCode))
            return (false, "Thiếu vnp_TxnRef.", null);

        var txn = await _paymentRepository.GetTransactionByOrderCodeAsync(orderCode);
        if (txn == null) return (false, "Không tìm thấy giao dịch.", null);

        // Idempotent: đã Paid rồi → OK
        if (txn.Status == FeePaymentStatus.Paid || txn.FeeInvoice.IsPaid)
            return (true, "Giao dịch đã được xác nhận trước đó.", txn);

        txn.RawCallback = string.Join("&", query.Select(kv => $"{kv.Key}={kv.Value}"));
        txn.UpdatedAt = DateTime.UtcNow;

        if (query.TryGetValue("vnp_TransactionNo", out var providerTxn))
            txn.ProviderTransactionId = providerTxn;

        var responseCode = query.TryGetValue("vnp_ResponseCode", out var rc) ? rc : "";
        if (responseCode == "00")
        {
            await MarkPaidAsync(txn, ProviderVnPay);
            return (true, isIpn ? "Confirm Success" : "Thanh toán thành công.", txn);
        }

        txn.Status = FeePaymentStatus.Failed;
        await _paymentRepository.UpdateTransactionAsync(txn);
        return (false, $"Thanh toán thất bại (code={responseCode}).", txn);
    }

    /// <summary>
    /// Đánh dấu giao dịch + hóa đơn Paid, sinh biên lai, thông báo PH/HS.
    /// </summary>
    private async Task MarkPaidAsync(PaymentTransaction txn, string method)
    {
        if (txn.Status == FeePaymentStatus.Paid && txn.FeeInvoice.IsPaid)
            return;

        var now = DateTime.UtcNow;
        txn.Status = FeePaymentStatus.Paid;
        txn.UpdatedAt = now;

        var invoice = txn.FeeInvoice;
        invoice.Status = FeePaymentStatus.Paid;
        invoice.IsPaid = true;
        invoice.PaidAt = now;
        invoice.PaymentMethod = method;
        invoice.TransactionId = txn.ProviderTransactionId ?? txn.OrderCode;
        invoice.ReceiptNumber ??= $"RC-{now:yyyyMMdd}-{invoice.Id:D6}";
        invoice.UpdatedAt = now;

        await _paymentRepository.UpdateTransactionAsync(txn);
        await _invoiceRepository.UpdateAsync(invoice);

        // Thông báo phụ huynh + học sinh
        var notifyIds = new List<int> { invoice.StudentId };
        var parentIds = await _context.StudentParents
            .Where(sp => sp.StudentId == invoice.StudentId)
            .Select(sp => sp.ParentId)
            .ToListAsync();
        notifyIds.AddRange(parentIds);

        await _notificationService.NotifyUsersAsync(
            notifyIds.Distinct().ToList(),
            "Thanh toán học phí thành công",
            $"Hóa đơn #{invoice.Id} ({invoice.FeeCategory?.Name ?? "học phí"}) đã thanh toán. Biên lai: {invoice.ReceiptNumber}.",
            sendPush: true);
    }

    /// <summary>
    /// PayOS trả orderCode dạng số; lúc tạo link ta dùng hash của OrderCode nội bộ.
    /// Tìm giao dịch Pending cùng provider có hash khớp.
    /// </summary>
    private async Task<PaymentTransaction?> FindTxnByPayOsNumericAsync(string rawOrder)
    {
        if (!long.TryParse(rawOrder, out var numeric))
            return null;

        var pending = await _context.PaymentTransactions
            .Include(t => t.FeeInvoice).ThenInclude(i => i.FeeCategory)
            .Include(t => t.FeeInvoice).ThenInclude(i => i.Student)
            .Where(t => t.Provider == ProviderPayOs && t.Status == FeePaymentStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync();

        return pending.FirstOrDefault(t =>
        {
            var h = Math.Abs(t.OrderCode.GetHashCode());
            if (h < 1_000_000) h += 1_000_000;
            return h == numeric;
        });
    }

    private static Dictionary<string, string> ParseConfig(string json)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
    /// <summary>Ẩn HashSecret / ApiKey / ChecksumKey khi trả về Admin GET.</summary>
    private static string MaskSecrets(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            var map = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var name = prop.Name;
                if (name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Key", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
                {
                    var val = prop.Value.GetString() ?? "";
                    map[name] = val.Length <= 4 ? "****" : val[..2] + "****" + val[^2..];
                }
                else
                {
                    map[name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.GetRawText()
                    };
                }
            }
            return JsonSerializer.Serialize(map);
        }
        catch
        {
            return "{}";
        }
    }

    private static PaymentTransactionDto MapTxn(PaymentTransaction t) => new()
    {
        Id = t.Id,
        FeeInvoiceId = t.FeeInvoiceId,
        Provider = t.Provider,
        OrderCode = t.OrderCode,
        ProviderTransactionId = t.ProviderTransactionId,
        Amount = t.Amount,
        Status = t.Status.ToString(),
        PaymentUrl = t.PaymentUrl,
        CreatedAt = t.CreatedAt
    };
}
