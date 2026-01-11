using Microsoft.AspNetCore.Mvc;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    [Area("RealEstateAdmin")]
    public class ReviewsController : Controller
    {
        public IActionResult Index()
        {
            var reviews = new List<dynamic>
            {
                new { Id = 1, User = "Nguyễn Văn A", Property = "Căn hộ Vinhomes Q7", Agent = "Môi giới A", Rating = 5, Comment = "Rất hài lòng với dịch vụ, nhân viên tư vấn nhiệt tình", Status = "Đã duyệt", CreatedDate = "15/01/2025 14:30" },
                new { Id = 2, User = "Trần Thị B", Property = "Nhà phố Thảo Điền", Agent = "Môi giới B", Rating = 4, Comment = "Tốt, nhưng cần cải thiện thời gian phản hồi", Status = "Chờ duyệt", CreatedDate = "14/01/2025 10:15" },
                new { Id = 3, User = "Lê Văn C", Property = "Biệt thự Quận 2", Agent = "Môi giới C", Rating = 3, Comment = "Bình thường", Status = "Đã ẩn", CreatedDate = "13/01/2025 16:20" }
            };

            return View(reviews);
        }
    }
}