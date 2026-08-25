from datetime import datetime

from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.base import Base
from domain.model.enum.Roomba.roomba_phase import RoombaPhase


class Roomba(Base):
    __tablename__ = "roomba"

    roomba_id: Mapped[int] = mapped_column(BigInteger, primary_key = True, autoincrement = True)
    device_id: Mapped[int] = mapped_column(BigInteger, ForeignKey("device.device_id"))
    phase: Mapped[str] = mapped_column(default = str(RoombaPhase.STOP))
    battery_percentage: Mapped[int] = mapped_column(default = 0)
    bin_full: Mapped[bool] = mapped_column(default = False)
    last_target: Mapped[str] = mapped_column(default = "")
    last_roomba_activation: Mapped[datetime] = mapped_column(default = datetime.min)
    last_roomba_end: Mapped[datetime] = mapped_column(default = datetime.min)
    last_clean_duration_minutes: Mapped[int] = mapped_column(default = 0)
    error_code: Mapped[int] = mapped_column(default = 0)
    error_message: Mapped[str] = mapped_column(default = "")
    pmap_id: Mapped[str] = mapped_column(default = "")
    user_pmapv_id: Mapped[str] = mapped_column(default = "")
    is_online: Mapped[bool] = mapped_column(default = False)
    last_seen: Mapped[datetime] = mapped_column(default = datetime.min)