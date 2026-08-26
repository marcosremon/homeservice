from datetime import datetime

from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.Base import Base
from domain.model.enum.Roomba.RoombaPhase import RoombaPhase

class Roomba(Base):
    __tablename__ = "roomba"

    roombaId: Mapped[int] = mapped_column("roomba_id", BigInteger, primary_key = True, autoincrement = True)
    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, ForeignKey("device.device_id"))
    phase: Mapped[str] = mapped_column(default = RoombaPhase.STOP.name)
    batteryPercent: Mapped[int] = mapped_column("battery_percent", default = 0)
    binFull: Mapped[bool] = mapped_column("bin_full", default = False)
    lastTarget: Mapped[str] = mapped_column("last_target", default = "")
    lastRoombaActivation: Mapped[datetime] = mapped_column("last_roomba_activation", default = datetime.min)
    lastRoombaEnd: Mapped[datetime] = mapped_column("last_roomba_end", default = datetime.min)
    lastCleanDurationMinutes: Mapped[int] = mapped_column("last_clean_duration_minutes", default = 0)
    errorCode: Mapped[int] = mapped_column("error_code", default = 0)
    errorMessage: Mapped[str] = mapped_column("error_message", default = "")
    pmapId: Mapped[str] = mapped_column("pmap_id", default = "")
    userPmapvId: Mapped[str] = mapped_column("user_pmapv_id", default = "")
    isOnline: Mapped[bool] = mapped_column("is_online", default = False)
    lastSeen: Mapped[datetime] = mapped_column("last_seen", default = datetime.min)