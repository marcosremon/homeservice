import asyncio
from contextlib import asynccontextmanager, suppress
from datetime import datetime, timedelta, timezone
from collections.abc import Sequence
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from domain.model.entity.RainSensor import RainSensor
from infraestructure.persistence.context.ApplicationDbContext import GetSession

_INTERVAL_SECONDS: int = 60
_RAIN_EXPIRY_MINUTES: int = 5

class RainSensorMonitor:
    def __init__(self) -> None:
        self._task: asyncio.Task[None] | None = None

    # region Start
    def Start(self) -> None:
        if self._task is None:
            self._task = asyncio.create_task(self._executeAsync(), name = "rain_sensor_monitor")
    # endregion

    # region Stop
    async def Stop(self) -> None:
        if self._task is None:
            return

        self._task.cancel()
        with suppress(asyncio.CancelledError):
            await self._task
        self._task = None
    # endregion

    # region _executeAsync
    async def _executeAsync(self) -> None:
        """
        El sleep va antes del trabajo, igual que en PresenceSensorMonitor: el primer
        chequeo no ocurre en el arranque sino pasado el intervalo.
        """
        while True:
            await asyncio.sleep(_INTERVAL_SECONDS)
            await self._runRainSensorJob()
    # endregion

    # region _runRainSensorJob
    @classmethod
    async def _runRainSensorJob(cls) -> None:
        try:
            async with asynccontextmanager(GetSession)() as session:
                await cls._expireRainStatus(session)
        except asyncio.CancelledError:
            raise
        except Exception as ex:
            print(f"error en RainSensorMonitor -> _runRainSensorJob {ex}")
            pass
    # endregion

    # region _expireRainStatus
    @staticmethod
    async def _expireRainStatus(session: AsyncSession) -> None:
        rainingSensors: Sequence[RainSensor] = (await session.scalars(select(RainSensor)
            .where(RainSensor.isRaining))).all()

        expiryLimit: datetime = datetime.now(timezone.utc).replace(tzinfo = None) - timedelta(minutes = _RAIN_EXPIRY_MINUTES)

        for rainSensor in rainingSensors:
            if rainSensor.lastDetectedRain < expiryLimit:
                rainSensor.isRaining = False

        await session.commit()
    # endregion