import asyncio
from contextlib import asynccontextmanager, suppress
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from domain.model.entity.RainSensor import RainSensor
from infraestructure.persistence.context.ApplicationDbContext import GetSession

_INTERVAL_SECONDS: int = 60

class RainSensorMonitor:
    def __init__(self) -> None:
        self._task: asyncio.Task[None] | None = None

    # region start
    def Start(self) -> None:
        if self._task is None:
            self._task = asyncio.create_task(self._executeAsync(), name = "rain_sensor_monitor")
    # endregion

    # region stop
    async def Stop(self) -> None:
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
        El sleep va antes del trabajo, igual que en PresenceSensorMonitor: el primer
        chequeo no ocurre en el arranque sino pasado el intervalo.
        """
        while True:
            await asyncio.sleep(_INTERVAL_SECONDS)
            await self._runRainSensorJob()
    # endregion

    # region _run_rain_sensor_job
    @classmethod
    async def _runRainSensorJob(cls) -> None:
        try:
            async with asynccontextmanager(GetSession)() as session:
                isRaining: bool = await cls._isRaining(session)

                if isRaining:
                    # TODO: decidir que se hace cuando llueve enviar notificacion a mi movil

                    pass
        except asyncio.CancelledError:
            raise
        except Exception:
            pass
    # endregion

    # region _is_raining
    @staticmethod
    async def _isRaining(session: AsyncSession) -> bool:
        """Llueve si cualquiera de los sensores de lluvia lo esta marcando."""
        rainSensor: RainSensor | None = await session.scalar(select(RainSensor)
            .where(RainSensor.isRaining == True).limit(1))

        return rainSensor is not None
    # endregion