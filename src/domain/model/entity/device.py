from sqlalchemy import ForeignKey, BigInteger
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.base import Base

class Device(Base):
    __tablename__ = "device"

    device_id: Mapped[int] = mapped_column(BigInteger, primary_key = True, autoincrement = True)
    house_zone_id: Mapped[int] = mapped_column(BigInteger, ForeignKey("house_zone.house_zone_id"))
    device_name: Mapped[str] = mapped_column(default = "")
    device_type: Mapped[str] = mapped_column(default = "")
    model: Mapped[str] = mapped_column(default = "")
    manufacturer: Mapped[str] = mapped_column(default = "")
    mac_address: Mapped[str] = mapped_column(default = "")