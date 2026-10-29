using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Event;
using HomeService.Application.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace HomeService.Infraestructure.BackgroundTasks.Job
{
    // Escucha RoombaActivatedEvent y guarda en base de datos cada cambio de estado
    // que publica RoombaUtils.
    public class RoombaActivationHandler : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ConcurrentDictionary<Task, byte> _pending = new ConcurrentDictionary<Task, byte>();

        public RoombaActivationHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        #region StartAsync
        public Task StartAsync(CancellationToken cancellationToken)
        {
            RoombaActivatedEvent.Subscribe(_handleRoombaActivation);
            return Task.CompletedTask;
        }
        #endregion

        #region StopAsync
        // Deja de escuchar y espera a las escrituras que ya estaban en marcha, salvo
        // que se agote el tiempo de parada del host.
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            RoombaActivatedEvent.Unsubscribe(_handleRoombaActivation);

            try
            {
                await Task.WhenAll(_pending.Keys).WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
        #endregion

        #region _handleRoombaActivation
        // Callback sincrono del evento: la escritura va al pool de hilos para no
        // bloquear a RoombaUtils, que publica el evento desde la conexion MQTT.
        private void _handleRoombaActivation(PatchRoombaStateRequest patchRoombaStateRequest)
        {
            Task task = Task.Run(() => _patchRoombaState(patchRoombaStateRequest));

            _pending.TryAdd(task, 0);
            task.ContinueWith(completed => _pending.TryRemove(completed, out _), TaskScheduler.Default);
        }
        #endregion

        #region _patchRoombaState
        private async Task _patchRoombaState(PatchRoombaStateRequest patchRoombaStateRequest)
        {
            try
            {
                // El handler es singleton y el repositorio scoped: fuera de una peticion
                // HTTP no hay scope, asi que se abre uno por escritura.
                await using AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
                IRoombaRepository roombaRepository = serviceScope.ServiceProvider.GetRequiredService<IRoombaRepository>();

                PatchRoombaStateResponse patchRoombaStateResponse = await roombaRepository.PatchRoombaState(patchRoombaStateRequest);

                if (!patchRoombaStateResponse.IsSuccess)
                    Console.WriteLine($"RoombaActivationHandler -> {patchRoombaStateResponse.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RoombaActivationHandler -> _patchRoombaState -> {ex.Message}");
            }
        }
        #endregion
    }
}
