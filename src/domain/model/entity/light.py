from datetime import datetime

from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import mapped_column, Mapped

from domain.model.entity.Base import Base
from domain.model.enum.Light.LightLocation import LightLocation

class Light(Base):
    __tablename__ = "light"

    lightId: Mapped[int] = mapped_column("light_id", BigInteger, primary_key = True, autoincrement = True)
    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, ForeignKey("device.device_id"))
    location: Mapped[str] = mapped_column(default = LightLocation.NONE.name)
    mqttTopic: Mapped[str] = mapped_column("mqtt_topic", default = "")
    isOn: Mapped[bool] = mapped_column("is_on", default = False)
    brightness: Mapped[int] = mapped_column(default = 0)
    color: Mapped[str] = mapped_column(default = "")
    colorTemperature: Mapped[int] = mapped_column("color_temperature", default = 0)
    lastStatusChange: Mapped[datetime] = mapped_column("last_status_change", default = datetime.min)
    isOnline: Mapped[bool] = mapped_column("is_online", default = False)
    lastSeen: Mapped[datetime] = mapped_column("last_seen", default = datetime.min)