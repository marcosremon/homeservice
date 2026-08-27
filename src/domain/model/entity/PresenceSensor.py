from datetime import datetime

from sqlalchemy import ForeignKey, BigInteger
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.Base import Base

class PresenceSensor(Base):
    __tablename__ = "presence_sensor"

    presenceSensorId: Mapped[int] = mapped_column("presence_sensor_id", BigInteger, primary_key = True, autoincrement = True)
    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, ForeignKey("device.device_id"))
    ts: Mapped[int] = mapped_column("ts", default = 0)
    presence: Mapped[bool] = mapped_column("presence", default = False)
    distanceCm: Mapped[int] = mapped_column("distance_cm", default = 0)
    motion: Mapped[str] = mapped_column("motion", default = "")
    lastDetectedPresence: Mapped[datetime] = mapped_column("last_detected_presence", default = datetime.min)