using Microsoft.AspNetCore.Mvc;

public class PagesController : Controller
{
    public IActionResult AboutUs ()
    {
        return View();
    }

    public IActionResult OurServices ()
    {
        return View();
    }

    public IActionResult Pricing ()
    {
        return View();
    }

    public IActionResult ContactUs ()
    {
        return View();
    }
    public IActionResult  FAQs ()
    {
        return View();
    }
    public IActionResult PrivacyPolicy ()
    {
        return View();
    }
}
