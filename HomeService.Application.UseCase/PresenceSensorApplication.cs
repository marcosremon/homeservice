using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;

namespace HomeService.Application.UseCase;

public class PresenceSensorApplication : IPresenceSensorApplication
{
    private readonly IPresenceSensorRepository _presenceSensorRepository;

    public PresenceSensorApplication(IPresenceSensorRepository presenceSensorRepository)
    {
        _presenceSensorRepository = presenceSensorRepository;
    }

    #region CreatePresenceSensor
    public Task<CreatePresenceSensorResponse> CreatePresenceSensor(CreatePresenceSensorRequest createPresenceSensorRequest) =>
        _presenceSensorRepository.CreatePresenceSensor(createPresenceSensorRequest);
    #endregion

    #region PatchPresenceSensorData
    public Task<PatchPresenceSensorDataResponse> PatchPresenceSensorData(PatchPresenceSensorDataRequest patchPresenceSensorDataRequest) =>
        _presenceSensorRepository.PatchPresenceSensorData(patchPresenceSensorDataRequest);
    #endregion
}
