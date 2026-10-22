using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;

namespace HomeService.Application.Interface.Application;

public interface IPresenceSensorApplication
{
    Task<CreatePresenceSensorResponse> CreatePresenceSensor(CreatePresenceSensorRequest createPresenceSensorRequest);

    Task<PatchPresenceSensorDataResponse> PatchPresenceSensorData(PatchPresenceSensorDataRequest patchPresenceSensorDataRequest);
}
