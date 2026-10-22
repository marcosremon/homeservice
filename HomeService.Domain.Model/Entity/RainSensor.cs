namespace HomeService.Domain.Model.Entity;

public class RainSensor
{
    public long RainSensorId { get; set; }
    public long DeviceId { get; set; }

    // valor crudo del adc del esp32, 0 = mojado, 4095 = seco (logica invertida)
    public int AdcValue { get; set; } = 4095;

    // el crudo normalizado con la calibracion seco/mojado, 0 = seco, 100 = mojado
    public int WetnessPercent { get; set; }

    // derivado de comparar WetnessPercent contra el umbral
    public bool IsRaining { get; set; }

    // momento de esta lectura, se actualiza siempre aunque no llueva
    public DateTime MeasureAt { get; set; } = DateTime.MinValue;

    // ultima vez que IsRaining fue true, no se pisa cuando escampa
    public DateTime LastDetectedRain { get; set; } = DateTime.MinValue;
}
