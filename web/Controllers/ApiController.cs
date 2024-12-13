using BL.Services;
using DTO;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    public class ApiController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IVaultService _vaultService;

        public ApiController(ILogger<HomeController> logger, IVaultService vaultService)
        {
            _logger = logger;
            _vaultService = vaultService;
        }

    }
}
