using System.Diagnostics;
using BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Helpers;
using web.Models;

namespace web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IVaultService _vaultService;

        public HomeController(ILogger<HomeController> logger, IVaultService vaultService)
        {
            _logger = logger;
            _vaultService = vaultService;
        }

        public async Task<IActionResult> Index()
        {
            var vault = await _vaultService.GetOrCreateVaultForUser(User.GetUserId());
            return View(vault);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
