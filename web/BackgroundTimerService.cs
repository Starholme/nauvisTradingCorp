using BL.Services;

namespace web
{
    public class BackgroundTimerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public BackgroundTimerService(IServiceScopeFactory serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int counter = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                counter++;
                // do async work
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    if (counter % 5 == 0)
                    {
                        var vaultService = scope.ServiceProvider.GetRequiredService<IVaultService>();
                        await vaultService.GetExportsFromInstances(stoppingToken);
                        await vaultService.GetImportRequestsFromInstances(stoppingToken);
                    }
                }
                if (counter > 599) counter = 0;
            }
        }
    }
}
