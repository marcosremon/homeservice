using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.GetPresenceSensorsStatus;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;

namespace HomeService.Application.Interface.Repository;

public interface IPresenceSensorRepository
{
    Task<CreatePresenceSensorResponse> CreatePresenceSensor(CreatePresenceSensorRequest createPresenceSensorRequest);

    Task<PatchPresenceSensorDataResponse> PatchPresenceSensorData(PatchPresenceSensorDataRequest patchPresenceSensorDataRequest);

    Task<GetPresenceSensorsStatusResponse> GetPresenceSensorsStatus();
}
