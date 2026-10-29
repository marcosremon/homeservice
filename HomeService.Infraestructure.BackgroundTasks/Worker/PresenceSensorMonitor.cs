using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.GetPresenceSensorsStatus;
using HomeService.Application.Interface.Repository;
using HomeService.Transversal.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeService.Infraestructure.BackgroundTasks.Worker
{
    // Cada 30 segundos mira si la casa esta vacia y, si lo esta, arranca el roomba.
    public class PresenceSensorMonitor : BackgroundService
    {
        private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PresenceSensorMonitor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        #region ExecuteAsync
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // WaitForNextTickAsync espera el intervalo antes de la primera vuelta: el
            // primer chequeo no ocurre en el arranque.
            using PeriodicTimer periodicTimer = new PeriodicTimer(_interval);

            while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
                await _runPresenceSensorJob();
        }
        #endregion

        #region _runPresenceSensorJob
        // El catch es lo que mantiene vivo el bucle: una excepcion que escapara de
        // ExecuteAsync pararia el host entero.
        private async Task _runPresenceSensorJob()
        {
            try
            {
                await using AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
                IPresenceSensorRepository presenceSensorRepository = serviceScope.ServiceProvider.GetRequiredService<IPresenceSensorRepository>();

                GetPresenceSensorsStatusResponse getPresenceSensorsStatusResponse = await presenceSensorRepository.GetPresenceSensorsStatus();

                if (getPresenceSensorsStatusResponse.IsSuccess && getPresenceSensorsStatusResponse.IsHouseEmpty)
                    await RoombaUtils.StartRoombaIfHouseIsEmpty(getPresenceSensorsStatusResponse.LastRoombaActivation);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PresenceSensorMonitor -> _runPresenceSensorJob -> {ex.Message}");
            }
        }
        #endregion
    }
}
