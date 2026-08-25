from datetime import datetime

from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import mapped_column, Mapped

from domain.model.entity.base import Base
from domain.model.enum.Light.light_location import LightLocation


class Light(Base):
    __tablename__ = "light"

    light_id: Mapped[int] = mapped_column(BigInteger, primary_key = True, autoincrement = True)
    device_id: Mapped[int] = mapped_column(BigInteger, ForeignKey("device.device_id"))
    location: Mapped[str] = mapped_column(default = str(LightLocation.NONE))
    is_on: Mapped[bool]
    brightness: Mapped[int]
    color: Mapped[str] = mapped_column(default = "")
    color_temperature: Mapped[int]
    last_status_change: Mapped[datetime] = mapped_column(default = datetime.min)
    is_online: Mapped[bool] = mapped_column(default = False)
    last_seen: Mapped[datetime] = mapped_column(default = datetime.min)