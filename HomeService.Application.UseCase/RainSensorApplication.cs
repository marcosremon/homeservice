using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.DataTransferObject.Notification.SendNotification;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Service;

namespace HomeService.Application.UseCase;

public class RainSensorApplication : IRainSensorApplication
{
    private readonly IRainSensorRepository _rainSensorRepository;
    private readonly INotificationService _notificationService;

    public RainSensorApplication(IRainSensorRepository rainSensorRepository, INotificationService notificationService)
    {
        _rainSensorRepository = rainSensorRepository;
        _notificationService = notificationService;
    }

    #region CreateRainSensor
    public Task<CreateRainSensorResponse> CreateRainSensor(CreateRainSensorRequest createRainSensorRequest) =>
        _rainSensorRepository.CreateRainSensor(createRainSensorRequest);
    #endregion

    #region PatchRainSensor
    public async Task<PatchRainSensorResponse> PatchRainSensor(PatchRainSensorRequest patchRainSensorRequest)
    {
        PatchRainSensorResponse patchRainSensorResponse = await _rainSensorRepository.PatchRainSensor(patchRainSensorRequest);

        if (patchRainSensorResponse.IsSuccess && patchRainSensorResponse.RainStarted)
        {
            SendNotificationRequest sendNotificationRequest = new SendNotificationRequest()
            {
                Title = "Esta lloviendo",
                Message = $"El sensor {patchRainSensorRequest.DeviceName} de {patchRainSensorRequest.CallOut} ha detectado lluvia.",
                Tags = "rain_cloud",
            };

            await _notificationService.SendNotification(sendNotificationRequest);
        }

        return patchRainSensorResponse;
    }
    #endregion
}
