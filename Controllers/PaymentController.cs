using HomeLengo.Models;
using HomeLengo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

namespace HomeLengo.Controllers
{
    public class PaymentController : Controller
    {
        private readonly HomeLengoContext _context;
        private readonly VNPayService _vnPayService;
        private readonly IConfiguration _configuration;

        public PaymentController(HomeLengoContext context, VNPayService vnPayService, IConfiguration configuration)
        {
            _context = context;
            _vnPayService = vnPayService;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("Payment/TestVNPay")]
        public IActionResult TestVNPay()
        {
            try
            {
                var tmnCode = _configuration["VNPay:TmnCode"] ?? "";
                var hashSecret = _configuration["VNPay:HashSecret"] ?? "";
                var url = _configuration["VNPay:Url"] ?? "";

                var testParams = new Dictionary<string, string>
                {
                    { "vnp_Amount", "1000000" },
                    { "vnp_Command", "pay" },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", "VND" },
                    { "vnp_IpAddr", "127.0.0.1" },
                    { "vnp_Locale", "vn" },
                    { "vnp_OrderInfo", "Test" },
                    { "vnp_OrderType", "other" },
                    { "vnp_ReturnUrl", "https://localhost:7031/Payment/PaymentCallback" },
                    { "vnp_TmnCode", tmnCode },
                    { "vnp_TxnRef", "TEST123" },
                    { "vnp_Version", "2.1.0" }
                };

                var sorted = testParams.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
                var signData = string.Join("&", sorted.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));

                return Json(new
                {
                    success = true,
                    config = new
                    {
                        tmnCode = string.IsNullOrEmpty(tmnCode) ? "MISSING" : $"{tmnCode.Substring(0, Math.Min(4, tmnCode.Length))}...",
                        hashSecret = string.IsNullOrEmpty(hashSecret) ? "MISSING" : $"{hashSecret.Substring(0, Math.Min(4, hashSecret.Length))}...",
                        url
                    },
                    testSignData = signData,
                    message = "Cấu hình VNPay đã được load (signData đã encode đúng chuẩn)"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int planId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán";
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdStr);

            var plan = await _context.ServicePlans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null) return NotFound("Không tìm thấy gói dịch vụ");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng";
                return RedirectToAction("Login", "Account");
            }

            // Tạo UserServicePackage mới cho mỗi lần checkout (tạm thời chưa active)
            // Điều này cho phép user mua lại gói hoặc nâng cấp
            var userPackage = new UserServicePackage
            {
                UserId = userId, // Lưu UserId của người đăng nhập
                PlanId = planId,
                StartDate = DateTime.Now,
                IsActive = false, // Chưa active, sẽ được kích hoạt khi thanh toán thành công
                CreatedAt = DateTime.Now
            };

            // Lưu vào database ngay lập tức
            _context.UserServicePackages.Add(userPackage);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Đã tạo UserServicePackage mới: ID={userPackage.Id}, UserId={userPackage.UserId}, PlanId={planId}, IsActive={userPackage.IsActive}");

            // Reload để có đầy đủ thông tin Plan
            userPackage = await _context.UserServicePackages
                .Include(usp => usp.Plan)
                .FirstOrDefaultAsync(usp => usp.Id == userPackage.Id);

            ViewBag.UserPackage = userPackage!;
            ViewBag.TotalAmount = plan.Price;
            ViewBag.UserPackageId = userPackage.Id;

            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> CreatePaymentUrl(int userPackageId)
        {
            try
            {
                if (userPackageId <= 0)
                    return Json(new { success = false, message = "Mã gói dịch vụ không hợp lệ" });

                var userPackage = await _context.UserServicePackages
                    .Include(usp => usp.Plan)
                    .FirstOrDefaultAsync(usp => usp.Id == userPackageId);

                if (userPackage == null)
                    return Json(new { success = false, message = "Không tìm thấy gói dịch vụ" });

                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thanh toán" });

                var userId = int.Parse(userIdStr);

                // Kiểm tra user có quyền với gói này không
                if (userPackage.UserId != userId)
                    return Json(new { success = false, message = "Bạn không có quyền thanh toán gói này" });

                // 1) Tạo Transaction
                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = userPackage.Plan.Price,
                    Currency = "VND",
                    TransactionType = "ServicePackage",
                    Status = "pending",
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // 2) TxnRef (<=100)
                var txnRef = $"USP{userPackageId}_{transaction.TransactionId}_{DateTime.Now:yyyyMMddHHmmss}";
                if (txnRef.Length > 100) txnRef = txnRef.Substring(0, 100);

                // 3) OrderInfo (<=255), sạch ký tự
                var orderInfo = $"Thanh toan goi dich vu {userPackage.Plan.Name}";
                orderInfo = Regex.Replace(orderInfo, @"[^a-zA-Z0-9\s-]", "");   // bỏ ký tự lạ
                orderInfo = Regex.Replace(orderInfo, @"\s+", " ").Trim();       // gộp space
                if (orderInfo.Length > 255) orderInfo = orderInfo.Substring(0, 255);

                // 4) IP (ép IPv4)
                var remoteIp = HttpContext.Connection.RemoteIpAddress;
                var ipAddress = remoteIp?.MapToIPv4().ToString() ?? "127.0.0.1";
                if (ipAddress == "::1") ipAddress = "127.0.0.1";

                // 5) ReturnUrl (ổn định)
                var returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentCallback";

                Console.WriteLine($"Order Info: {orderInfo}");
                Console.WriteLine($"TxnRef: {txnRef}");
                Console.WriteLine($"IP Address: {ipAddress}");
                Console.WriteLine($"Return URL: {returnUrl}");

                // 6) Params gửi VNPay
                // Lưu ý: amount phải x100 và là số nguyên
                var amountVnp = Convert.ToInt64(decimal.Round(userPackage.Plan.Price * 100, 0, MidpointRounding.AwayFromZero));

                var vnp_Params = new Dictionary<string, string>
                {
                    { "vnp_TxnRef", txnRef },
                    { "vnp_OrderInfo", orderInfo },
                    { "vnp_OrderType", "other" },
                    { "vnp_Amount", amountVnp.ToString() }
                };

                // 7) Create URL
                var paymentUrl = _vnPayService.CreatePaymentUrl(vnp_Params, returnUrl, ipAddress);

                // 8) Lưu Payment
                var payment = new Payment
                {
                    TransactionId = transaction.TransactionId,
                    Provider = "VNPay",
                    ProviderRef = txnRef,
                    Status = "initiated",
                    PaidAt = null
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // 9) Lưu session phục vụ callback
                HttpContext.Session.SetString($"Payment_{txnRef}_TransactionId", transaction.TransactionId.ToString());
                HttpContext.Session.SetString($"Payment_{txnRef}_UserPackageId", userPackageId.ToString());

                Console.WriteLine($"Session saved for TxnRef: {txnRef}");

                return Json(new { success = true, paymentUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreatePaymentUrl Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
                if (string.IsNullOrEmpty(vnp_SecureHash))
                {
                    ViewBag.Success = false;
                    ViewBag.Message = "Thiếu chữ ký bảo mật";
                    return View();
                }

                // Lấy params (đã bị ASP.NET decode sẵn)
                var vnp_Params = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var key in Request.Query.Keys)
                {
                    if (!string.IsNullOrEmpty(key)
                        && key.StartsWith("vnp_", StringComparison.Ordinal)
                        && key != "vnp_SecureHash"
                        && key != "vnp_SecureHashType")
                    {
                        var value = Request.Query[key].ToString();
                        if (!string.IsNullOrEmpty(value))
                            vnp_Params[key] = value;
                    }
                }

                Console.WriteLine("=== VNPay Callback Debug ===");
                foreach (var param in vnp_Params.OrderBy(x => x.Key, StringComparer.Ordinal))
                    Console.WriteLine($"  {param.Key} = {param.Value}");
                Console.WriteLine($"Received Hash: {vnp_SecureHash}");

                // Validate signature (VNPayService phải encode đúng khi verify)
                var isValidSignature = _vnPayService.ValidateSignature(vnp_Params, vnp_SecureHash);
                Console.WriteLine($"Signature Valid: {isValidSignature}");
                Console.WriteLine("===========================");

                if (!isValidSignature)
                {
                    ViewBag.Success = false;
                    ViewBag.Message = "Chữ ký không hợp lệ";
                    return View();
                }

                var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
                var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();
                var vnp_TransactionNo = Request.Query["vnp_TransactionNo"].ToString();

                var transactionIdStr = HttpContext.Session.GetString($"Payment_{vnp_TxnRef}_TransactionId");
                var userPackageIdStr = HttpContext.Session.GetString($"Payment_{vnp_TxnRef}_UserPackageId");

                if (string.IsNullOrEmpty(transactionIdStr) || string.IsNullOrEmpty(userPackageIdStr))
                {
                    ViewBag.Success = false;
                    ViewBag.Message = "Không tìm thấy thông tin giao dịch (session bị mất hoặc sai TxnRef)";
                    return View();
                }

                var transactionId = int.Parse(transactionIdStr);
                var userPackageId = int.Parse(userPackageIdStr);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == transactionId && p.ProviderRef == vnp_TxnRef);

                if (payment == null)
                {
                    ViewBag.Success = false;
                    ViewBag.Message = "Không tìm thấy thông tin thanh toán";
                    return View();
                }

                // Update
                payment.ProviderRef = $"{vnp_TxnRef}_{vnp_TransactionNo}";
                payment.Status = vnp_ResponseCode == "00" ? "completed" : "failed";
                if (vnp_ResponseCode == "00") payment.PaidAt = DateTime.Now;

                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction != null)
                    transaction.Status = vnp_ResponseCode == "00" ? "completed" : "failed";

                await _context.SaveChangesAsync();

                // Clear session
                HttpContext.Session.Remove($"Payment_{vnp_TxnRef}_TransactionId");
                HttpContext.Session.Remove($"Payment_{vnp_TxnRef}_UserPackageId");

                if (vnp_ResponseCode == "00")
                {
                    // Kích hoạt UserServicePackage khi thanh toán thành công
                    var userPackage = await _context.UserServicePackages
                        .Include(usp => usp.Plan)
                        .FirstOrDefaultAsync(usp => usp.Id == userPackageId);
                    
                    if (userPackage != null)
                    {
                        // Vô hiệu hóa các gói cũ của user
                        var oldPackages = await _context.UserServicePackages
                            .Where(usp => usp.UserId == userPackage.UserId && usp.Id != userPackageId && usp.IsActive == true)
                            .ToListAsync();
                        
                        foreach (var oldPackage in oldPackages)
                        {
                            oldPackage.IsActive = false;
                            oldPackage.EndDate = DateTime.Now;
                        }

                        // Kích hoạt gói mới
                        userPackage.IsActive = true;
                        userPackage.StartDate = DateTime.Now;
                        
                        // Đảm bảo lưu vào database
                        await _context.SaveChangesAsync();
                        
                        Console.WriteLine($"UserServicePackage #{userPackageId} đã được kích hoạt và lưu vào database.");
                        Console.WriteLine($"UserId: {userPackage.UserId}, Plan: {userPackage.Plan?.Name}, IsActive: {userPackage.IsActive}, StartDate: {userPackage.StartDate}");
                    }
                    else
                    {
                        Console.WriteLine($"Không tìm thấy UserServicePackage với ID: {userPackageId}");
                    }

                    ViewBag.Success = true;
                    ViewBag.Message = "Thanh toán thành công! Gói dịch vụ của bạn đã được kích hoạt.";
                    ViewBag.UserPackageId = userPackageId;
                    ViewBag.TransactionId = transactionId;
                }
                else
                {
                    ViewBag.Success = false;
                    ViewBag.Message = $"Thanh toán thất bại. Mã lỗi: {vnp_ResponseCode}";
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PaymentCallback Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ViewBag.Success = false;
                ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý";
                return View();
            }
        }
    }
}
