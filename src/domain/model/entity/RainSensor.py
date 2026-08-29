from datetime import datetime
from sqlalchemy import BigInteger, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column
from domain.model.entity.Base import Base

class RainSensor(Base):
    __tablename__ = "rain_sensor"

    rainSensorId: Mapped[int] = mapped_column("rain_sensor_id", BigInteger, primary_key = True, autoincrement = True)
    deviceId: Mapped[int] = mapped_column("device_id", BigInteger, ForeignKey("device.device_id"))
    # valor crudo del adc del esp32, 0 = mojado, 4095 = seco (logica invertida)
    adcValue: Mapped[int] = mapped_column("adc_value", default = 4095)
    # el crudo normalizado con la calibracion seco/mojado, 0 = seco, 100 = mojado
    wetnessPercent: Mapped[int] = mapped_column("wetness_percent", default = 0)
    # derivado de comparar wetness_percent contra el umbral
    isRaining: Mapped[bool] = mapped_column("is_raining", default = False)
    # momento de esta lectura, se actualiza siempre aunque no llueva
    measureAt: Mapped[datetime] = mapped_column("measure_at", default = datetime.min)
    # ultima vez que is_raining fue true, no se pisa cuando escampa
    lastDetectedRain: Mapped[datetime] = mapped_column("last_detected_rain", default = datetime.min)