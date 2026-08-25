from sqlalchemy import BigInteger
from sqlalchemy.orm import Mapped, mapped_column

from domain.model.entity.base import Base

class HouseZone(Base):
    __tablename__ = "house_zone"

    house_zone_id: Mapped[int] = mapped_column(BigInteger, primary_key = True, autoincrement = True)
    callout: Mapped[str] = mapped_column(default = "")