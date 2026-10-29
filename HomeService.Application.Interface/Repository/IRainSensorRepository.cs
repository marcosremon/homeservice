using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.GetRainingSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;

namespace HomeService.Application.Interface.Repository;

public interface IRainSensorRepository
{
    Task<CreateRainSensorResponse> CreateRainSensor(CreateRainSensorRequest createRainSensorRequest);

    Task<PatchRainSensorResponse> PatchRainSensor(PatchRainSensorRequest patchRainSensorRequest);

    Task<GetRainingSensorResponse> GetRainingSensor();

    // Pone IsRaining = false en los sensores que no han vuelto a detectar lluvia
    // desde expiryLimit. Lo usa RainSensorMonitor.
    Task ExpireRainStatus(DateTime expiryLimit);
}
