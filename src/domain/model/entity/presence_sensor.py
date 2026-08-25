from datetime import datetime

from sqlalchemy import ForeignKey, BigInteger
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.base import Base

class PresenceSensor(Base):
    __tablename__ = "presence_sensor"

    presence_sensor_id: Mapped[int] = mapped_column(BigInteger, primary_key = True, autoincrement = True)
    device_id: Mapped[int] = mapped_column(BigInteger, ForeignKey("device.device_id"))
    ts: Mapped[int] = mapped_column(default = 0)
    presence: Mapped[bool] = mapped_column(default = False)
    distance_cm: Mapped[int] = mapped_column(default = 0)
    motion: Mapped[str] = mapped_column(default = "")
    last_detected_presence: Mapped[datetime] = mapped_column(default = datetime.min)