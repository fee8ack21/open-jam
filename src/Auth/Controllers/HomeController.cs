using System.Diagnostics;
using Auth.Models;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
