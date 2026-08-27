from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column
from domain.model.entity.Base import Base

class TemperatureSensor(Base):
    __tablename__ = 'temperature_sensor'

    temperatureSensorId: Mapped[int] = mapped_column("temperature_sensor_id", BigInteger, primary_key = True, autoincrement = True)
    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, ForeignKey("device.device_id"))
    temperature: Mapped[float] = mapped_column("temperature_celsius", default = 0.0)
    # este es el valor crudo del sensor adc significa analogic to digital es el valor antes de convertirlo a celsius
    adcVoltage: Mapped[float] = mapped_column("adc_voltage", default = 0.0)
    # esto es para ver cuando fue la ultima medicion de temperatura
    measureAt: Mapped[float] = mapped_column("measure_at", default = 0.0)