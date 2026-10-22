using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;

namespace HomeService.Application.UseCase;

public class TemperatureSensorApplication : ITemperatureSensorApplication
{
    private readonly ITemperatureSensorRepository _temperatureSensorRepository;

    public TemperatureSensorApplication(ITemperatureSensorRepository temperatureSensorRepository) =>
        _temperatureSensorRepository = temperatureSensorRepository;

    #region CreateTemperatureSensor
    public Task<CreateTemperatureSensorResponse> CreateTemperatureSensor(CreateTemperatureSensorRequest createTemperatureSensorRequest) =>
        _temperatureSensorRepository.CreateTemperatureSensor(createTemperatureSensorRequest);
    #endregion

    #region PatchTemperatureSensor
    public Task<PatchTemperatureSensorResponse> PatchTemperatureSensor(PatchTemperatureSensorRequest patchTemperatureSensorRequest) =>
        _temperatureSensorRepository.PatchTemperatureSensor(patchTemperatureSensorRequest);
    #endregion
}
