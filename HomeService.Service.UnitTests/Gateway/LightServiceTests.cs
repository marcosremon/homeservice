using HomeService.Application.DataTransferObject.Light;
using HomeService.Application.DataTransferObject.Light.GetLightByLocation;
using HomeService.Application.DataTransferObject.Light.PatchLightStatus;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Service;
using HomeService.Domain.Model.Enum.Light;
using HomeService.Infraestructure.Gateway;
using HomeService.Transversal.Wrapper.Base;
using Moq;

namespace HomeService.Service.UnitTests.Gateway
{
    public class LightServiceTests
    {
        private readonly Mock<IMqttService> _mqttService = new Mock<IMqttService>();
        private readonly Mock<ILightRepository> _lightRepository = new Mock<ILightRepository>();
        private readonly LightService _lightService;

        public LightServiceTests()
        {
            _lightService = new LightService(_mqttService.Object, _lightRepository.Object);
        }

        #region _setupLight
        private void _setupLight(bool isOn, string mqttTopic = "casa/salon/luz")
        {
            _lightRepository.Setup(repository => repository.GetLightByLocation(It.Is<GetLightByLocationRequest>(request => request.Location == LightLocation.LIVING_ROOM)))
                .ReturnsAsync(new GetLightByLocationResponse
                {
                    ResponseCode = ResponseCodes.OK,
                    IsSuccess = true,
                    LightDto = new LightDto { Location = LightLocation.LIVING_ROOM, IsOn = isOn, MqttTopic = mqttTopic },
                });
        }
        #endregion

        #region ExecuteLightOrder_UnknownOrder_DoesNothing
        [Theory]
        [InlineData("light_order_encender_luces_cocina")]
        [InlineData("")]
        public async Task ExecuteLightOrder_UnknownOrder_DoesNothing(string intentName)
        {
            string message = await _lightService.ExecuteLightOrder(intentName);

            Assert.Equal("Orden no reconocida", message);
            _mqttService.VerifyNoOtherCalls();
            _lightRepository.VerifyNoOtherCalls();
        }
        #endregion

        #region ExecuteLightOrder_LightNotFound_SaysSo
        [Fact]
        public async Task ExecuteLightOrder_LightNotFound_SaysSo()
        {
            _lightRepository.Setup(repository => repository.GetLightByLocation(It.IsAny<GetLightByLocationRequest>()))
                .ReturnsAsync(new GetLightByLocationResponse { ResponseCode = ResponseCodes.NOT_FOUND, IsSuccess = false });

            string message = await _lightService.ExecuteLightOrder("light_order_encender_luces_salon");

            Assert.Equal("No encuentro la luz de LIVING_ROOM", message);
            _mqttService.VerifyNoOtherCalls();
        }
        #endregion

        #region ExecuteLightOrder_TurnOn_PublishesOnAndSavesState
        [Fact]
        public async Task ExecuteLightOrder_TurnOn_PublishesOnAndSavesState()
        {
            _setupLight(isOn: false);

            string message = await _lightService.ExecuteLightOrder("light_order_encender_luces_salon");

            Assert.Equal("Encendiendo la luz de LIVING_ROOM", message);
            _mqttService.Verify(service => service.Publish("casa/salon/luz", "ON", true), Times.Once);
            _lightRepository.Verify(repository => repository.PatchLightStatus(It.Is<PatchLightStatusRequest>(request =>
                request.LightLocation == LightLocation.LIVING_ROOM && request.IsOn)), Times.Once);
        }
        #endregion

        #region ExecuteLightOrder_TurnOff_PublishesOffAndSavesState
        [Fact]
        public async Task ExecuteLightOrder_TurnOff_PublishesOffAndSavesState()
        {
            _setupLight(isOn: true);

            string message = await _lightService.ExecuteLightOrder("light_order_apagar_luces_salon");

            Assert.Equal("Apagando la luz de LIVING_ROOM", message);
            _mqttService.Verify(service => service.Publish("casa/salon/luz", "OFF", true), Times.Once);
            _lightRepository.Verify(repository => repository.PatchLightStatus(It.Is<PatchLightStatusRequest>(request => !request.IsOn)), Times.Once);
        }
        #endregion

        #region ExecuteLightOrder_AlreadyInRequestedState_DoesNotPublish
        [Theory]
        [InlineData("light_order_encender_luces_salon", true, "La luz de LIVING_ROOM ya esta encendida")]
        [InlineData("light_order_apagar_luces_salon", false, "La luz de LIVING_ROOM ya esta apagada")]
        public async Task ExecuteLightOrder_AlreadyInRequestedState_DoesNotPublish(string intentName, bool isOn, string expectedMessage)
        {
            _setupLight(isOn);

            string message = await _lightService.ExecuteLightOrder(intentName);

            Assert.Equal(expectedMessage, message);
            _mqttService.VerifyNoOtherCalls();
            _lightRepository.Verify(repository => repository.PatchLightStatus(It.IsAny<PatchLightStatusRequest>()), Times.Never);
        }
        #endregion

        #region ExecuteLightOrder_WithoutMqttTopic_UsesDefaultTopic
        [Fact]
        public async Task ExecuteLightOrder_WithoutMqttTopic_UsesDefaultTopic()
        {
            _setupLight(isOn: false, mqttTopic: string.Empty);

            await _lightService.ExecuteLightOrder("light_order_encender_luces_salon");

            _mqttService.Verify(service => service.Publish("home/living_room/lights/cmd", "ON", true), Times.Once);
        }
        #endregion

        #region ExecuteLightOrder_PublishFails_DoesNotSaveState
        // Pendiente en CLAUDE.md: el estado solo llega a la BBDD si la publicacion sale.
        [Fact]
        public async Task ExecuteLightOrder_PublishFails_DoesNotSaveState()
        {
            _setupLight(isOn: false);
            _mqttService.Setup(service => service.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ThrowsAsync(new Exception("broker caido"));

            await Assert.ThrowsAsync<Exception>(() => _lightService.ExecuteLightOrder("light_order_encender_luces_salon"));

            _lightRepository.Verify(repository => repository.PatchLightStatus(It.IsAny<PatchLightStatusRequest>()), Times.Never);
        }
        #endregion
    }
}
