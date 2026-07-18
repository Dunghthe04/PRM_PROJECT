using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Api.Common;

/// <summary>
/// Tạo URL thanh toán + verify chữ ký HMAC-SHA512 của VNPay (FR4.2).
/// Không lưu thông tin thẻ — chỉ xử lý tham số cổng.
/// </summary>
public static class VnPayHelper
{
    /// <summary>
    /// Build URL checkout VNPay từ các tham số (đã có vnp_SecureHash).
    /// </summary>
    public static string BuildPaymentUrl(
        string paymentBaseUrl,
        SortedDictionary<string, string> parameters,
        string hashSecret)
    {
        var signData = BuildSignData(parameters);
        var secureHash = HmacSha512(hashSecret, signData);
        parameters["vnp_SecureHash"] = secureHash;

        var query = string.Join("&",
            parameters.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

        var separator = paymentBaseUrl.Contains('?') ? "&" : "?";
        return paymentBaseUrl + separator + query;
    }

    /// <summary>
    /// Verify chữ ký từ query return/IPN.
    /// Loại bỏ vnp_SecureHash / vnp_SecureHashType trước khi hash.
    /// </summary>
    public static bool ValidateSignature(
        IDictionary<string, string> allParams,
        string hashSecret)
    {
        if (!allParams.TryGetValue("vnp_SecureHash", out var receivedHash)
            || string.IsNullOrWhiteSpace(receivedHash))
            return false;

        var filtered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in allParams)
        {
            if (kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                || kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(kv.Value))
                filtered[kv.Key] = kv.Value;
        }

        var signData = BuildSignData(filtered);
        var computed = HmacSha512(hashSecret, signData);
        return string.Equals(computed, receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Số tiền VND × 100 theo quy ước VNPay (không có phần thập phân).</summary>
    public static long ToVnPayAmount(decimal amountVnd)
        => (long)Math.Round(amountVnd * 100m, MidpointRounding.AwayFromZero);

    /// <summary>Format thời gian VNPay: yyyyMMddHHmmss (GMT+7).</summary>
    public static string FormatCreateDate(DateTime utcNow)
    {
        var vn = TimeZoneInfo.ConvertTimeFromUtc(utcNow,
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"));
        return vn.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }

    private static string BuildSignData(SortedDictionary<string, string> parameters)
        => string.Join("&",
            parameters.Select(kv => $"{kv.Key}={kv.Value}"));

    private static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
