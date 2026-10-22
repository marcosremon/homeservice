namespace HomeService.Domain.Model.Entity;

public class TemperatureSensor
{
    public long TemperatureSensorId { get; set; }
    public long DeviceId { get; set; }

    // la columna se llama temperature_celsius, no temperature
    public double Temperature { get; set; }

    // valor crudo del sensor; adc es analogic to digital, el valor antes de pasarlo a celsius
    public double AdcVoltage { get; set; }

    // cuando fue la ultima medicion de temperatura
    public double MeasureAt { get; set; }
}
