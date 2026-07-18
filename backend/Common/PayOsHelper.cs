using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Api.Common;

/// <summary>
/// Tạo chữ ký / verify checksum HMAC-SHA256 của PayOS (FR4.2).
/// Không lưu thông tin thẻ — chỉ checksum payload.
/// </summary>
public static class PayOsHelper
{
    /// <summary>
    /// Chữ ký tạo link thanh toán (amount, cancelUrl, description, orderCode, returnUrl).
    /// Theo tài liệu PayOS: nối key=value theo alphabet rồi HMAC-SHA256.
    /// </summary>
    public static string SignCreatePayment(
        long orderCode,
        int amount,
        string description,
        string cancelUrl,
        string returnUrl,
        string checksumKey)
    {
        var raw =
            $"amount={amount}&cancelUrl={cancelUrl}&description={description}" +
            $"&orderCode={orderCode}&returnUrl={returnUrl}";
        return HmacSha256Hex(checksumKey, raw);
    }

    /// <summary>
    /// Verify webhook PayOS: hash dữ liệu trong field data theo alphabet.
    /// </summary>
    public static bool ValidateWebhookSignature(
        JsonElement dataElement,
        string receivedSignature,
        string checksumKey)
    {
        if (string.IsNullOrWhiteSpace(receivedSignature))
            return false;

        var pairs = new SortedDictionary<string, string>(StringComparer.Ordinal);
        FlattenJson("", dataElement, pairs);

        var raw = string.Join("&", pairs.Select(kv => $"{kv.Key}={kv.Value}"));
        var computed = HmacSha256Hex(checksumKey, raw);
        return string.Equals(computed, receivedSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>PayOS amount là số nguyên VND (không ×100).</summary>
    public static int ToPayOsAmount(decimal amountVnd)
        => (int)Math.Round(amountVnd, MidpointRounding.AwayFromZero);

    private static void FlattenJson(string prefix, JsonElement element, SortedDictionary<string, string> pairs)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenJson(key, prop.Value, pairs);
                }
                break;
            case JsonValueKind.Array:
                // PayOS data thường là object; bỏ qua array phức tạp
                pairs[prefix] = element.GetRawText();
                break;
            case JsonValueKind.String:
                pairs[prefix] = element.GetString() ?? "";
                break;
            case JsonValueKind.Number:
                pairs[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                pairs[prefix] = element.GetBoolean() ? "true" : "false";
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                pairs[prefix] = "";
                break;
        }
    }

    private static string HmacSha256Hex(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
