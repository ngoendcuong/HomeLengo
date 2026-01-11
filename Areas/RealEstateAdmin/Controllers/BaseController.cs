using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeLengo.Areas.RealEstateAdmin.Controllers
{
    public class BaseController : Controller
    {
        protected int? GetRoleId()
        {
            var roleIdStr = HttpContext.Session.GetString("RoleId");
            if (int.TryParse(roleIdStr, out int roleId))
            {
                return roleId;
            }
            return null;
        }

        protected bool IsAdmin()
        {
            return GetRoleId() == 1;
        }

        protected bool IsModerator()
        {
            return GetRoleId() == 4;
        }

        protected bool HasAccess()
        {
            var roleId = GetRoleId();
            return roleId == 1 || roleId == 4;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = RedirectToAction("Index", "Home", new { area = "" });
                return;
            }

            // Kiểm tra quyền truy cập RealEstateAdmin
            if (!HasAccess())
            {
                context.Result = RedirectToAction("Index", "Home", new { area = "" });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

