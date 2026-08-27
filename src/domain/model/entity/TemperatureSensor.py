from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column
from domain.model.entity.Base import Base

class TemperatureSensor(Base):
    __tablename__ = 'temperature_sensor'

    temperatureSensorId: Mapped[int] = mapped_column("temperature_sensor_id", BigInteger, primary_key = True, autoincrement = True)
    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, ForeignKey("device.device_id"))
    ts: Mapped[int] = mapped_column("ts", default = 0)
    temperatureCelsius: Mapped[float] = mapped_column("temperature_celsius", default = 0)
    humidityPercent: Mapped[float] = mapped_column("humidity_percent", default = 0)
    temperatureVolts: Mapped[float] = mapped_column("temperature_volts", default = 0)
    humidityVolts: Mapped[float] = mapped_column("humidity_volts", default = 0)