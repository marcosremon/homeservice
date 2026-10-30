using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.DataTransferObject.Notification.SendNotification;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Service;
using HomeService.Application.UseCase;
using HomeService.Transversal.Wrapper.Base;
using Moq;

namespace HomeService.Service.UnitTests.UseCase
{
    public class RainSensorApplicationTests
    {
        private readonly Mock<IRainSensorRepository> _rainSensorRepository = new Mock<IRainSensorRepository>();
        private readonly Mock<INotificationService> _notificationService = new Mock<INotificationService>();
        private readonly RainSensorApplication _application;

        public RainSensorApplicationTests()
        {
            _notificationService.Setup(service => service.SendNotification(It.IsAny<SendNotificationRequest>()))
                .ReturnsAsync(new SendNotificationResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true });

            _application = new RainSensorApplication(_rainSensorRepository.Object, _notificationService.Object);
        }

        #region _patchRequest
        private static PatchRainSensorRequest _patchRequest() => new PatchRainSensorRequest
        {
            CallOut = "terraza",
            DeviceName = "lluvia-terraza",
            DeviceType = "rain_sensor",
            IsRaining = true,
        };
        #endregion

        #region CreateRainSensor_DelegatesToRepositoryWithoutNotifying
        [Fact]
        public async Task CreateRainSensor_DelegatesToRepositoryWithoutNotifying()
        {
            CreateRainSensorRequest request = new CreateRainSensorRequest { IsRaining = true };
            CreateRainSensorResponse response = new CreateRainSensorResponse();
            _rainSensorRepository.Setup(repository => repository.CreateRainSensor(request)).ReturnsAsync(response);

            Assert.Same(response, await _application.CreateRainSensor(request));
            _notificationService.Verify(service => service.SendNotification(It.IsAny<SendNotificationRequest>()), Times.Never);
        }
        #endregion

        #region PatchRainSensor_RainStarts_SendsNotification
        [Fact]
        public async Task PatchRainSensor_RainStarts_SendsNotification()
        {
            SendNotificationRequest? sent = null;
            _notificationService.Setup(service => service.SendNotification(It.IsAny<SendNotificationRequest>()))
                .Callback<SendNotificationRequest>(request => sent = request)
                .ReturnsAsync(new SendNotificationResponse { IsSuccess = true });

            PatchRainSensorResponse response = new PatchRainSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, RainStarted = true };
            _rainSensorRepository.Setup(repository => repository.PatchRainSensor(It.IsAny<PatchRainSensorRequest>())).ReturnsAsync(response);

            PatchRainSensorResponse result = await _application.PatchRainSensor(_patchRequest());

            Assert.Same(response, result);
            Assert.NotNull(sent);
            Assert.Equal("Esta lloviendo", sent.Title);
            Assert.Equal("El sensor lluvia-terraza de terraza ha detectado lluvia.", sent.Message);
            Assert.Equal("rain_cloud", sent.Tags);
        }
        #endregion

        #region PatchRainSensor_NoNotificationWhenRainDidNotStart
        // Solo el paso de seco a mojado avisa: seguir lloviendo o dejar de llover no.
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task PatchRainSensor_NoNotificationWhenRainDidNotStart(bool isSuccess, bool rainStarted)
        {
            _rainSensorRepository.Setup(repository => repository.PatchRainSensor(It.IsAny<PatchRainSensorRequest>()))
                .ReturnsAsync(new PatchRainSensorResponse { IsSuccess = isSuccess, RainStarted = rainStarted });

            await _application.PatchRainSensor(_patchRequest());

            _notificationService.Verify(service => service.SendNotification(It.IsAny<SendNotificationRequest>()), Times.Never);
        }
        #endregion

        #region PatchRainSensor_NotificationFails_StillReturnsRepositoryResponse
        [Fact]
        public async Task PatchRainSensor_NotificationFails_StillReturnsRepositoryResponse()
        {
            _notificationService.Setup(service => service.SendNotification(It.IsAny<SendNotificationRequest>()))
                .ReturnsAsync(new SendNotificationResponse { ResponseCode = ResponseCodes.UNEXPECTED_ERROR, IsSuccess = false });

            PatchRainSensorResponse response = new PatchRainSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, RainStarted = true };
            _rainSensorRepository.Setup(repository => repository.PatchRainSensor(It.IsAny<PatchRainSensorRequest>())).ReturnsAsync(response);

            PatchRainSensorResponse result = await _application.PatchRainSensor(_patchRequest());

            Assert.Same(response, result);
            Assert.True(result.IsSuccess);
        }
        #endregion
    }
}
