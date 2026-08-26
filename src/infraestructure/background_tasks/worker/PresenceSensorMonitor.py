import asyncio
from contextlib import asynccontextmanager, suppress

from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.GetPresenceSensorsStatusResponse import GetPresenceSensorsStatusResponse
from application.interface.repository.IEventRepository import IEventRepository
from infraestructure.gateway.roomba import RoombaUtils
from infraestructure.persistence.context.ApplicationDbContext import GetSession
from infraestructure.persistence.dependencies.DependencyInjection import BuildEventRepository

_INTERVAL_SECONDS = 30

class PresenceSensorMonitor:
    def __init__(self) -> None:
        self._task: asyncio.Task[None] | None = None

    # region start
    def start(self) -> None:
        if self._task is None:
            self._task = asyncio.create_task(self._executeAsync(), name = "presence_sensor_monitor")
    # endregion

    # region stop
    async def stop(self) -> None:
        if self._task is None:
            return

        self._task.cancel()
        with suppress(asyncio.CancelledError):
            await self._task
        self._task = None
    # endregion

    # region _execute_async
    async def _executeAsync(self) -> None:
        """
        El sleep va antes del trabajo, igual que WaitForNextTickAsync: el primer
        chequeo no ocurre en el arranque sino pasado el intervalo.
        """
        while True:
            await asyncio.sleep(_INTERVAL_SECONDS)
            await self._runPresenceSensorJob()
    # endregion

    # region _run_presence_sensor_job
    async def _runPresenceSensorJob(self) -> None:
        try:
            # HTTP no hay scope, hay que abrir la sesion a mano y cerrarla aqui.
            async with asynccontextmanager(GetSession)() as session:
                eventRepository: IEventRepository = BuildEventRepository(session)

                getPresenceSensorsStatusResponse: GetPresenceSensorsStatusResponse = await eventRepository.GetPresenceSensorsStatus()

                if getPresenceSensorsStatusResponse.isSuccess and getPresenceSensorsStatusResponse.isHouseEmpty:
                    await RoombaUtils.StartRoombaIfHouseIsEmpty(getPresenceSensorsStatusResponse.lastRoombaActivation)
        except asyncio.CancelledError:
            # Nunca tragarse la cancelacion: es la señal de parada del stop().
            raise
        except Exception:
            pass
    # endregion