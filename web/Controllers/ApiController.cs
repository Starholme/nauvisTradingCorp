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

        public async Task<IActionResult> ExportFromInstance (ExportFromInstanceDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return Problem("Invalid model");
            }
            _logger.LogInformation("Recieved items from instance: " + dto.InstanceId + " count: " + dto.Items.Count);

            await _vaultService.AddItemsToVault(dto);

            return Ok();
        }
    }
}
