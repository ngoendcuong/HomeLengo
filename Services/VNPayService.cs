using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HomeLengo.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _url;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _tmnCode = _configuration["VNPay:TmnCode"] ?? "";
            _hashSecret = _configuration["VNPay:HashSecret"] ?? "";
            _url = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            if (string.IsNullOrEmpty(_tmnCode) || string.IsNullOrEmpty(_hashSecret))
            {
                throw new InvalidOperationException(
                    "VNPay configuration is missing. Please check TmnCode and HashSecret in appsettings.json");
            }
        }

        public string CreatePaymentUrl(Dictionary<string, string> vnp_Params, string returnUrl, string? ipAddress = null)
        {
            // 1) Gom params hợp lệ
            var requestData = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var param in vnp_Params)
            {
                if (!string.IsNullOrWhiteSpace(param.Value) && param.Key.StartsWith("vnp_", StringComparison.Ordinal))
                    requestData[param.Key] = param.Value;
            }

            // 2) Params bắt buộc (ghi đè nếu có)
            requestData["vnp_Version"] = "2.1.0";
            requestData["vnp_Command"] = "pay";
            requestData["vnp_TmnCode"] = _tmnCode;
            requestData["vnp_CurrCode"] = "VND";
            requestData["vnp_OrderType"] = requestData.ContainsKey("vnp_OrderType") ? requestData["vnp_OrderType"] : "other";
            requestData["vnp_ReturnUrl"] = returnUrl;
            requestData["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? GetIpAddress() : ipAddress!;
            requestData["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss");
            requestData["vnp_Locale"] = "vn";

            // 3) Bỏ hash nếu lỡ truyền vào
            requestData.Remove("vnp_SecureHash");
            requestData.Remove("vnp_SecureHashType");

            // 4) Lọc rỗng + sort
            var sortedParams = requestData
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            Console.WriteLine("=== CreatePaymentUrl Debug ===");
            Console.WriteLine("Params before hash (sorted):");
            foreach (var p in sortedParams)
                Console.WriteLine($"  {p.Key} = {p.Value}");

            // 5) SIGN DATA: phải URL-encode đồng bộ
            var signData = BuildQueryString(sortedParams, encode: true);
            Console.WriteLine($"SignData(ENCODED): {signData}");
            Console.WriteLine($"HashSecret: {_hashSecret.Substring(0, Math.Min(10, _hashSecret.Length))}...");

            var vnp_SecureHash = HmacSHA512(_hashSecret, signData);

            Console.WriteLine($"Generated Hash: {vnp_SecureHash}");
            Console.WriteLine("=============================");

            // 6) Query string gửi đi: dùng đúng chuỗi đã encode + thêm hash (hash không encode)
            var queryString = signData + $"&vnp_SecureHash={vnp_SecureHash}";
            var fullUrl = $"{_url}?{queryString}";

            Console.WriteLine($"Payment URL Length: {fullUrl.Length} chars");
            return fullUrl;
        }

        public bool ValidateSignature(Dictionary<string, string> vnp_Params, string vnp_SecureHash)
        {
            // 1) Lọc params hợp lệ (bỏ SecureHash & SecureHashType)
            var paramsWithoutHash = vnp_Params
                .Where(kv =>
                    kv.Key.StartsWith("vnp_", StringComparison.Ordinal) &&
                    kv.Key != "vnp_SecureHash" &&
                    kv.Key != "vnp_SecureHashType" &&
                    !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            // 2) Sort
            var sortedParams = paramsWithoutHash
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            // 3) SIGN DATA verify: encode y chang lúc tạo URL
            var signData = BuildQueryString(sortedParams, encode: true);

            Console.WriteLine("=== ValidateSignature Debug ===");
            Console.WriteLine($"SignData(ENCODED): {signData}");
            Console.WriteLine($"HashSecret: {_hashSecret.Substring(0, Math.Min(10, _hashSecret.Length))}...");

            var calculatedHash = HmacSHA512(_hashSecret, signData);

            Console.WriteLine($"Calculated Hash: {calculatedHash}");
            Console.WriteLine($"Received Hash: {vnp_SecureHash}");
            Console.WriteLine($"Match: {string.Equals(calculatedHash, vnp_SecureHash, StringComparison.OrdinalIgnoreCase)}");
            Console.WriteLine("==============================");

            return string.Equals(calculatedHash, vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> items, bool encode)
        {
            // VNPay cần query dạng: key=value&key=value..., encode theo chuẩn URL query
            return string.Join("&", items.Select(kv =>
            {
                var k = encode ? WebUtility.UrlEncode(kv.Key) : kv.Key;
                var v = encode ? WebUtility.UrlEncode(kv.Value) : kv.Value;
                return $"{k}={v}";
            }));
        }

        private static string HmacSHA512(string key, string inputData)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
            return BitConverter.ToString(hashValue).Replace("-", "").ToLowerInvariant();
        }

        private static string GetIpAddress()
        {
            // Service không có HttpContext => fallback
            return "127.0.0.1";
        }
    }
}
