using HomeService.Application.Interface.Repository;
using HomeService.Transversal.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeService.Infraestructure.BackgroundTasks.Worker
{
    // Cada minuto da por terminada la lluvia de los sensores que llevan mas de 5
    // minutos sin volver a detectarla.
    public class RainSensorMonitor : BackgroundService
    {
        private static readonly TimeSpan _interval = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan _rainExpiry = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RainSensorMonitor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        #region ExecuteAsync
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer periodicTimer = new PeriodicTimer(_interval);

            while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
                await _runRainSensorJob();
        }
        #endregion

        #region _runRainSensorJob
        private async Task _runRainSensorJob()
        {
            try
            {
                await using AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
                IRainSensorRepository rainSensorRepository = serviceScope.ServiceProvider.GetRequiredService<IRainSensorRepository>();

                await rainSensorRepository.ExpireRainStatus(GeneralUtils.UtcNow() - _rainExpiry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RainSensorMonitor -> _runRainSensorJob -> {ex.Message}");
            }
        }
        #endregion
    }
}
