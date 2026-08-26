import asyncio
from contextlib import asynccontextmanager, suppress

from application.data_transfer_object.home_automation.sensor.presence_sensor.get_presence_sensors_status.get_presence_sensors_status_response import GetPresenceSensorsStatusResponse
from application.interface.repository.i_event_repository import IEventRepository
from infraestructure.gateway.roomba import roomba_utils
from infraestructure.persistence.context.application_db_context import get_session
from infraestructure.persistence.dependencies.dependency_injection import build_event_repository

_INTERVAL_SECONDS = 30

class PresenceSensorMonitor:
    def __init__(self) -> None:
        self._task: asyncio.Task[None] | None = None

    # region start
    def start(self) -> None:
        if self._task is None:
            self._task = asyncio.create_task(self._execute_async(), name = "presence_sensor_monitor")
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
    async def _execute_async(self) -> None:
        """
        El sleep va antes del trabajo, igual que WaitForNextTickAsync: el primer
        chequeo no ocurre en el arranque sino pasado el intervalo.
        """
        while True:
            await asyncio.sleep(_INTERVAL_SECONDS)
            await self._run_presence_sensor_job()
    # endregion

    # region _run_presence_sensor_job
    async def _run_presence_sensor_job(self) -> None:
        try:
            # HTTP no hay scope, hay que abrir la sesion a mano y cerrarla aqui.
            async with asynccontextmanager(get_session)() as session:
                event_repository: IEventRepository = build_event_repository(session)

                get_presence_sensors_status_response: GetPresenceSensorsStatusResponse = await event_repository.get_presence_sensors_status()

                if get_presence_sensors_status_response.is_success and get_presence_sensors_status_response.is_house_empty:
                    await roomba_utils.start_roomba_if_house_is_empty(get_presence_sensors_status_response.last_roomba_activation)
        except asyncio.CancelledError:
            # Nunca tragarse la cancelacion: es la señal de parada del stop().
            raise
        except Exception:
            pass
    # endregion