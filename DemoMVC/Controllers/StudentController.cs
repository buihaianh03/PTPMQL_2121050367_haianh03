using Microsoft.AspNetCore.Mvc;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    [Route("sinh-vien")]
    public class StudentController : Controller
    {
        [Route("trang-chu")]
        public IActionResult Index()
        {
            ViewBag.Title = "Thông tin sinh viên";

            ViewData["Message"] = "Nhập thông tin sinh viên";

            return View("~/Views/Lesson4/Student/Index.cshtml");
        }

        [HttpPost]
        [Route("trang-chu")]
        public IActionResult Index(Student std)
        {
            ViewBag.FullName = std.FullName;
            ViewBag.Address = std.Address;
            ViewBag.University = std.University;

            ViewData["Message"] = "Thông tin sinh viên đã nhập";

             return View("~/Views/Lesson4/Student/Index.cshtml");
        }
    }
}