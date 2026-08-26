from sqlalchemy import BigInteger
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.Base import Base

class HouseZone(Base):
    __tablename__ = "house_zone"

    houseZoneId: Mapped[int] = mapped_column("house_zone_id", BigInteger, primary_key = True, autoincrement = True)
    callout: Mapped[str] = mapped_column(default = "")