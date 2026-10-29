using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;

namespace HomeService.Application.Interface.Application;

public interface IRainSensorApplication
{
    Task<CreateRainSensorResponse> CreateRainSensor(CreateRainSensorRequest createRainSensorRequest);
    Task<PatchRainSensorResponse> PatchRainSensor(PatchRainSensorRequest patchRainSensorRequest);
}
