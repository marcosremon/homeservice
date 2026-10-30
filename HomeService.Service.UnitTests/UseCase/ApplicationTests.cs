using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Service;
using HomeService.Application.UseCase;
using Moq;

namespace HomeService.Service.UnitTests.UseCase
{
    // Las aplicaciones que solo delegan: mismo objeto de ida y de vuelta.
    public class ApplicationTests
    {
        #region AlexaApplication_DelegatesToService
        [Fact]
        public async Task AlexaApplication_DelegatesToService()
        {
            AlexaRequest alexaRequest = new AlexaRequest();
            AlexaResponse alexaResponse = new AlexaResponse();
            Mock<IAlexaService> alexaService = new Mock<IAlexaService>();
            alexaService.Setup(service => service.SendAlexaOrder(alexaRequest)).ReturnsAsync(alexaResponse);

            AlexaResponse result = await new AlexaApplication(alexaService.Object).SendAlexaOrder(alexaRequest);

            Assert.Same(alexaResponse, result);
        }
        #endregion

        #region ChangeComputerStatusApplication_DelegatesToRepository
        [Fact]
        public async Task ChangeComputerStatusApplication_DelegatesToRepository()
        {
            GetComputerStatusResponse getComputerStatusResponse = new GetComputerStatusResponse();
            TurnOnComputerResponse turnOnComputerResponse = new TurnOnComputerResponse();
            TurnOffComputerResponse turnOffComputerResponse = new TurnOffComputerResponse();

            Mock<IChangeComputerStatusRepository> repository = new Mock<IChangeComputerStatusRepository>();
            repository.Setup(r => r.GetComputerStatus()).ReturnsAsync(getComputerStatusResponse);
            repository.Setup(r => r.TurnOnComputer()).ReturnsAsync(turnOnComputerResponse);
            repository.Setup(r => r.TurnOffComputer()).ReturnsAsync(turnOffComputerResponse);

            ChangeComputerStatusApplication application = new ChangeComputerStatusApplication(repository.Object);

            Assert.Same(getComputerStatusResponse, await application.GetComputerStatus());
            Assert.Same(turnOnComputerResponse, await application.TurnOnComputer());
            Assert.Same(turnOffComputerResponse, await application.TurnOffComputer());
        }
        #endregion

        #region PresenceSensorApplication_DelegatesToRepository
        [Fact]
        public async Task PresenceSensorApplication_DelegatesToRepository()
        {
            CreatePresenceSensorRequest createRequest = new CreatePresenceSensorRequest();
            CreatePresenceSensorResponse createResponse = new CreatePresenceSensorResponse();
            PatchPresenceSensorDataRequest patchRequest = new PatchPresenceSensorDataRequest();
            PatchPresenceSensorDataResponse patchResponse = new PatchPresenceSensorDataResponse();

            Mock<IPresenceSensorRepository> repository = new Mock<IPresenceSensorRepository>();
            repository.Setup(r => r.CreatePresenceSensor(createRequest)).ReturnsAsync(createResponse);
            repository.Setup(r => r.PatchPresenceSensorData(patchRequest)).ReturnsAsync(patchResponse);

            PresenceSensorApplication application = new PresenceSensorApplication(repository.Object);

            Assert.Same(createResponse, await application.CreatePresenceSensor(createRequest));
            Assert.Same(patchResponse, await application.PatchPresenceSensorData(patchRequest));
        }
        #endregion

        #region RoombaApplication_DelegatesToRepository
        [Fact]
        public async Task RoombaApplication_DelegatesToRepository()
        {
            CreateRoombaRequest createRequest = new CreateRoombaRequest();
            CreateRoombaResponse createResponse = new CreateRoombaResponse();
            PatchRoombaStateRequest patchRequest = new PatchRoombaStateRequest();
            PatchRoombaStateResponse patchResponse = new PatchRoombaStateResponse();

            Mock<IRoombaRepository> repository = new Mock<IRoombaRepository>();
            repository.Setup(r => r.CreateRoomba(createRequest)).ReturnsAsync(createResponse);
            repository.Setup(r => r.PatchRoombaState(patchRequest)).ReturnsAsync(patchResponse);

            RoombaApplication application = new RoombaApplication(repository.Object);

            Assert.Same(createResponse, await application.CreateRoomba(createRequest));
            Assert.Same(patchResponse, await application.PatchRoombaState(patchRequest));
        }
        #endregion

        #region TemperatureSensorApplication_DelegatesToRepository
        [Fact]
        public async Task TemperatureSensorApplication_DelegatesToRepository()
        {
            CreateTemperatureSensorRequest createRequest = new CreateTemperatureSensorRequest();
            CreateTemperatureSensorResponse createResponse = new CreateTemperatureSensorResponse();
            PatchTemperatureSensorRequest patchRequest = new PatchTemperatureSensorRequest();
            PatchTemperatureSensorResponse patchResponse = new PatchTemperatureSensorResponse();

            Mock<ITemperatureSensorRepository> repository = new Mock<ITemperatureSensorRepository>();
            repository.Setup(r => r.CreateTemperatureSensor(createRequest)).ReturnsAsync(createResponse);
            repository.Setup(r => r.PatchTemperatureSensor(patchRequest)).ReturnsAsync(patchResponse);

            TemperatureSensorApplication application = new TemperatureSensorApplication(repository.Object);

            Assert.Same(createResponse, await application.CreateTemperatureSensor(createRequest));
            Assert.Same(patchResponse, await application.PatchTemperatureSensor(patchRequest));
        }
        #endregion
    }
}
