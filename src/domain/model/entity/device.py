from sqlalchemy import ForeignKey, BigInteger
from sqlalchemy.orm import Mapped, mapped_column
from domain.model.entity.base import Base

class Device(Base):
    __tablename__ = "device"

    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, primary_key = True, autoincrement = True)
    houseZoneId: Mapped[int] = mapped_column("house_zone_id", BigInteger, ForeignKey("house_zone.house_zone_id"))
    deviceName: Mapped[str] = mapped_column("device_name", default = "")
    deviceType: Mapped[str] = mapped_column(__name_pos="device_type", default = "")
    model: Mapped[str] = mapped_column("model", default = "")
    manufacturer: Mapped[str] = mapped_column("manufacturer", default = "")
    macAddress: Mapped[str] = mapped_column("mac_address", default = "")