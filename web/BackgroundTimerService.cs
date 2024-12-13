using BL.Services;

namespace web
{
    public class BackgroundTimerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public BackgroundTimerService(IServiceScopeFactory serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // do async work
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var vaultService = scope.ServiceProvider.GetRequiredService<IVaultService>();
                    await vaultService.GetExportsFromInstances(stoppingToken);
                }
            }
        }
    }
}
