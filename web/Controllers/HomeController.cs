using System.Diagnostics;
using BL.Services;
using DTO;
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
        private readonly IClusterioService _clusterioService;

        public HomeController(ILogger<HomeController> logger, IVaultService vaultService, IClusterioService clusterioService)
        {
            _logger = logger;
            _vaultService = vaultService;
            _clusterioService = clusterioService;
        }

        public async Task<IActionResult> Index()
        {
            var dto = new HomeDTO();
            dto.Vault = await _vaultService.GetOrCreateVaultForUser(User.GetUserId());
            var instance = await _clusterioService.GetInstanceStatus("1176834574");
            dto.InstanceStatus = instance != null ? instance.Status : "Unknown";
            return View(dto);
        }

        public async Task<IActionResult> StartInstance() {
            var instanceId = "1176834574";
            return Ok();
            

            //Call to clusterio
            

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
