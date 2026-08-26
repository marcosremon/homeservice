import asyncio
from contextlib import asynccontextmanager, suppress

from infraestructure.persistence.context.application_db_context import get_session

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
        """
        El try/except vacio es intencionado, como en .NET: si una vuelta falla no
        se puede dejar morir la Task, porque nadie la reiniciaria.
        """
        try:
            # HTTP no hay scope, hay que abrir la sesion a mano y cerrarla aqui.
            async with asynccontextmanager(get_session)() as session:
                # TODO: IEventRepository.GetPresenceSensorsStatus(session) y, si
                # is_success e is_house_empty, RoombaUtils.start_roomba_if_house_is_empty
                # (last_roomba_activation). Ninguno de los dos existe todavia en Python.
                _ = session
        except asyncio.CancelledError:
            # Nunca tragarse la cancelacion: es la senal de parada del stop().
            raise
        except Exception:
            pass
    # endregion