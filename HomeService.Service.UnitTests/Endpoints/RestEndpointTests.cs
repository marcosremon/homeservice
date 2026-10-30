using System.Net;
using System.Text.Json;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Wrapper.Base;
using Moq;

namespace HomeService.Service.UnitTests.Endpoints
{
    // Un test por endpoint REST con el cuerpo tal cual lo mandan los ESP32: comprueba
    // la ruta, las claves JSON de entrada (callout frente a callOut) y las de salida.
    public class RestEndpointTests : EndpointTestBase
    {
        private const string _presenceBody = "{\"callout\":\"salon\",\"deviceName\":\"presencia-salon\",\"deviceType\":\"presence_sensor\",\"ts\":1730000000,\"presence\":true,\"distanceCm\":120,\"motion\":\"moving\",\"model\":\"LD2410\",\"manufacturer\":\"HLK\",\"macAddress\":\"AA:BB:CC:DD:EE:FF\"}";
        private const string _rainCreateBody = "{\"callOut\":\"terraza\",\"deviceName\":\"lluvia\",\"deviceType\":\"rain_sensor\",\"model\":\"YL-83\",\"manufacturer\":\"Generic\",\"macAddress\":\"AA:00\",\"adcValue\":1800,\"wetnessPercent\":55,\"isRaining\":true}";
        private const string _rainPatchBody = "{\"callOut\":\"terraza\",\"deviceName\":\"lluvia\",\"deviceType\":\"rain_sensor\",\"model\":\"YL-83\",\"macAddress\":\"AA:00\",\"adcValue\":1200,\"wetnessPercent\":70,\"isRaining\":true}";
        private const string _roombaCreateBody = "{\"callout\":\"salon\",\"deviceName\":\"roomba\",\"deviceType\":\"roomba\",\"model\":\"i7\",\"manufacturer\":\"iRobot\",\"macAddress\":\"50:14\"}";
        private const string _roombaPatchBody = "{\"isActivation\":true,\"isFinished\":false,\"target\":\"kitchen\",\"phase\":\"run\",\"batteryPercent\":90,\"binFull\":false,\"errorCode\":0,\"errorMessage\":\"ninguno\",\"pmapId\":\"p1\",\"userPmapvId\":\"v1\",\"isOnline\":true}";
        private const string _temperatureCreateBody = "{\"callOut\":\"cocina\",\"deviceName\":\"temp\",\"deviceType\":\"temperature_sensor\",\"model\":\"LM35\",\"manufacturer\":\"TI\",\"macAddress\":\"AA:01\",\"temperature\":21.5,\"adcVoltage\":0.215,\"measureAt\":1730000000}";
        private const string _temperaturePatchBody = "{\"callOut\":\"cocina\",\"deviceName\":\"temp\",\"deviceType\":\"temperature_sensor\",\"model\":\"LM35\",\"macAddress\":\"AA:01\",\"temperature\":22.5}";

        public RestEndpointTests(HomeServiceWebApplicationFactory factory) : base(factory) { }

        #region _assertBaseResponse
        private static async Task<JsonElement> _assertBaseResponse(HttpResponseMessage response, HttpStatusCode statusCode, int responseCodeJson, bool isSuccess, string message)
        {
            Assert.Equal(statusCode, response.StatusCode);
            AssertJsonContentType(response);

            JsonElement json = await ReadJson(response);
            Assert.Equal(responseCodeJson, json.GetProperty("responseCodeJson").GetInt32());
            Assert.Equal(isSuccess, json.GetProperty("isSuccess").GetBoolean());
            Assert.Equal(message, json.GetProperty("message").GetString());

            return json;
        }
        #endregion

        #region TurnOnComputer_Returns200
        [Fact]
        public async Task TurnOnComputer_Returns200()
        {
            Factory.ChangeComputerStatusApplication.Setup(application => application.TurnOnComputer())
                .ReturnsAsync(new TurnOnComputerResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "PC already on. Nothing to do." });

            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/turn-on-computer");

            await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "PC already on. Nothing to do.");
        }
        #endregion

        #region TurnOffComputer_Returns200
        [Fact]
        public async Task TurnOffComputer_Returns200()
        {
            Factory.ChangeComputerStatusApplication.Setup(application => application.TurnOffComputer())
                .ReturnsAsync(new TurnOffComputerResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "Apagando el ordenador (CachyOS via SSH)." });

            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/turn-off-computer");

            await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "Apagando el ordenador (CachyOS via SSH).");
        }
        #endregion

        #region GetComputerStatus_ReturnsComputerStatusWithCapitalKey
        // "ComputerStatus" con mayuscula: es la clave que espera el cliente.
        [Fact]
        public async Task GetComputerStatus_ReturnsComputerStatusWithCapitalKey()
        {
            Factory.ChangeComputerStatusApplication.Setup(application => application.GetComputerStatus())
                .ReturnsAsync(new GetComputerStatusResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, ComputerStatus = true, Message = "ON" });

            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/get-computer-status");

            JsonElement json = await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "ON");
            Assert.True(json.GetProperty("ComputerStatus").GetBoolean());
        }
        #endregion

        #region GetComputerStatus_ApplicationError_Returns500
        [Fact]
        public async Task GetComputerStatus_ApplicationError_Returns500()
        {
            Factory.ChangeComputerStatusApplication.Setup(application => application.GetComputerStatus())
                .ReturnsAsync(new GetComputerStatusResponse { ResponseCode = ResponseCodes.UNEXPECTED_ERROR, IsSuccess = false, Message = "The ip address is empty on settings (LAN_COMPUTER_IP)" });

            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/get-computer-status");

            await _assertBaseResponse(response, HttpStatusCode.InternalServerError, 998, false, "The ip address is empty on settings (LAN_COMPUTER_IP)");
        }
        #endregion

        #region CreatePresenceSensor_BindsLowercaseCallout
        [Fact]
        public async Task CreatePresenceSensor_BindsLowercaseCallout()
        {
            CreatePresenceSensorRequest? captured = null;
            Factory.PresenceSensorApplication.Setup(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()))
                .Callback<CreatePresenceSensorRequest>(request => captured = request)
                .ReturnsAsync(new CreatePresenceSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "created" });

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/sensor/presence-sensor/create-presence-sensor", _presenceBody);

            await _assertBaseResponse(response, HttpStatusCode.Created, 201, true, "created");
            Assert.NotNull(captured);
            Assert.Equal("salon", captured.CallOut);
            Assert.Equal("presencia-salon", captured.DeviceName);
            Assert.Equal(1730000000, captured.Ts);
            Assert.True(captured.Presence);
            Assert.Equal(120, captured.DistanceCm);
            Assert.Equal("AA:BB:CC:DD:EE:FF", captured.MacAddress);
        }
        #endregion

        #region PatchPresenceSensorData_Returns200
        [Fact]
        public async Task PatchPresenceSensorData_Returns200()
        {
            PatchPresenceSensorDataRequest? captured = null;
            Factory.PresenceSensorApplication.Setup(application => application.PatchPresenceSensorData(It.IsAny<PatchPresenceSensorDataRequest>()))
                .Callback<PatchPresenceSensorDataRequest>(request => captured = request)
                .ReturnsAsync(new PatchPresenceSensorDataResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "updated" });

            HttpResponseMessage response = await Send(HttpMethod.Patch, "/api/sensor/presence-sensor/patch-presence-sensor-data", _presenceBody);

            await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "updated");
            Assert.Equal("salon", captured!.CallOut);
            Assert.Equal("moving", captured.Motion);
        }
        #endregion

        #region CreateRainSensor_BindsCamelCaseCallOut
        [Fact]
        public async Task CreateRainSensor_BindsCamelCaseCallOut()
        {
            CreateRainSensorRequest? captured = null;
            Factory.RainSensorApplication.Setup(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>()))
                .Callback<CreateRainSensorRequest>(request => captured = request)
                .ReturnsAsync(new CreateRainSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "created" });

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/sensors/rain-sensor/create-rain-sensor", _rainCreateBody);

            await _assertBaseResponse(response, HttpStatusCode.Created, 201, true, "created");
            Assert.Equal("terraza", captured!.CallOut);
            Assert.Equal(1800, captured.AdcValue);
            Assert.Equal(55, captured.WetnessPercent);
            Assert.True(captured.IsRaining);
        }
        #endregion

        #region CreateRainSensor_Conflict_Returns409
        [Fact]
        public async Task CreateRainSensor_Conflict_Returns409()
        {
            Factory.RainSensorApplication.Setup(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>()))
                .ReturnsAsync(new CreateRainSensorResponse { ResponseCode = ResponseCodes.CONFLICT, IsSuccess = false, Message = "already exists" });

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/sensors/rain-sensor/create-rain-sensor", _rainCreateBody);

            await _assertBaseResponse(response, HttpStatusCode.Conflict, 409, false, "already exists");
        }
        #endregion

        #region PatchRainSensor_ReturnsRainStarted
        [Fact]
        public async Task PatchRainSensor_ReturnsRainStarted()
        {
            Factory.RainSensorApplication.Setup(application => application.PatchRainSensor(It.IsAny<PatchRainSensorRequest>()))
                .ReturnsAsync(new PatchRainSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, RainStarted = true, Message = "updated" });

            HttpResponseMessage response = await Send(HttpMethod.Patch, "/api/sensors/rain-sensor/patch-rain-sensor", _rainPatchBody);

            JsonElement json = await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "updated");
            Assert.True(json.GetProperty("rainStarted").GetBoolean());
        }
        #endregion

        #region CreateRoomba_Returns201
        [Fact]
        public async Task CreateRoomba_Returns201()
        {
            CreateRoombaRequest? captured = null;
            Factory.RoombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()))
                .Callback<CreateRoombaRequest>(request => captured = request)
                .ReturnsAsync(new CreateRoombaResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "roomba created" });

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/roomba/create-roomba", _roombaCreateBody);

            await _assertBaseResponse(response, HttpStatusCode.Created, 201, true, "roomba created");
            Assert.Equal("salon", captured!.CallOut);
            Assert.Equal("iRobot", captured.Manufacturer);
        }
        #endregion

        #region CreateRoomba_AlreadyExists_Returns409With994
        [Fact]
        public async Task CreateRoomba_AlreadyExists_Returns409With994()
        {
            Factory.RoombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()))
                .ReturnsAsync(new CreateRoombaResponse { ResponseCode = ResponseCodes.ANY_ROOMBA_EXIST, IsSuccess = false, Message = "Any roomba exists on bbdd" });

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/roomba/create-roomba", _roombaCreateBody);

            await _assertBaseResponse(response, HttpStatusCode.Conflict, 994, false, "Any roomba exists on bbdd");
        }
        #endregion

        #region PatchRoombaState_ParsesTargetAndPhase
        [Fact]
        public async Task PatchRoombaState_ParsesTargetAndPhase()
        {
            PatchRoombaStateRequest? captured = null;
            Factory.RoombaApplication.Setup(application => application.PatchRoombaState(It.IsAny<PatchRoombaStateRequest>()))
                .Callback<PatchRoombaStateRequest>(request => captured = request)
                .ReturnsAsync(new PatchRoombaStateResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "updated" });

            HttpResponseMessage response = await Send(HttpMethod.Patch, "/api/roomba/patch-roomba-state", _roombaPatchBody);

            await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "updated");
            Assert.Equal(RoombaTarget.KITCHEN, captured!.Target);
            Assert.Equal(RoombaPhase.RUN, captured.Phase);
            Assert.Equal(90, captured.BatteryPercent);
            Assert.True(captured.IsActivation);
        }
        #endregion

        #region CreateTemperatureSensor_Returns201
        [Fact]
        public async Task CreateTemperatureSensor_Returns201()
        {
            CreateTemperatureSensorRequest? captured = null;
            Factory.TemperatureSensorApplication.Setup(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>()))
                .Callback<CreateTemperatureSensorRequest>(request => captured = request)
                .ReturnsAsync(new CreateTemperatureSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "created" });

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/sensors/temperature-sensor/create-temperature-sensor", _temperatureCreateBody);

            await _assertBaseResponse(response, HttpStatusCode.Created, 201, true, "created");
            Assert.Equal(21.5, captured!.Temperature);
            Assert.Equal(0.215, captured.AdcVoltage);
            Assert.Equal(1730000000, captured.MeasureAt);
        }
        #endregion

        #region PatchTemperatureSensor_OmittedReadingsArriveAsNull
        [Fact]
        public async Task PatchTemperatureSensor_OmittedReadingsArriveAsNull()
        {
            PatchTemperatureSensorRequest? captured = null;
            Factory.TemperatureSensorApplication.Setup(application => application.PatchTemperatureSensor(It.IsAny<PatchTemperatureSensorRequest>()))
                .Callback<PatchTemperatureSensorRequest>(request => captured = request)
                .ReturnsAsync(new PatchTemperatureSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "updated" });

            HttpResponseMessage response = await Send(HttpMethod.Patch, "/api/sensors/temperature-sensor/patch-temperature-sensor", _temperaturePatchBody);

            await _assertBaseResponse(response, HttpStatusCode.OK, 200, true, "updated");
            Assert.Equal(22.5, captured!.Temperature);
            Assert.Null(captured.AdcVoltage);
            Assert.Null(captured.MeasureAt);
        }
        #endregion

        #region ApplicationThrows_Returns500WithoutStackTrace
        [Fact]
        public async Task ApplicationThrows_Returns500WithoutStackTrace()
        {
            Factory.RoombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()))
                .ThrowsAsync(new InvalidOperationException("conexion rechazada"));

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/roomba/create-roomba", _roombaCreateBody);

            await _assertBaseResponse(response, HttpStatusCode.InternalServerError, 500, false, "Ha ocurrido un error al crear el roomba conexion rechazada.");
        }
        #endregion

        #region MalformedJson_Returns400WithoutCallingApplication
        [Theory]
        [InlineData("/api/sensor/presence-sensor/create-presence-sensor")]
        [InlineData("/api/sensors/rain-sensor/create-rain-sensor")]
        [InlineData("/api/roomba/create-roomba")]
        [InlineData("/api/sensors/temperature-sensor/create-temperature-sensor")]
        public async Task MalformedJson_Returns400WithoutCallingApplication(string url)
        {
            HttpResponseMessage response = await Send(HttpMethod.Post, url, "{ esto no es json");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Factory.PresenceSensorApplication.Verify(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()), Times.Never);
            Factory.RainSensorApplication.Verify(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>()), Times.Never);
            Factory.RoombaApplication.Verify(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()), Times.Never);
            Factory.TemperatureSensorApplication.Verify(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>()), Times.Never);
        }
        #endregion

        #region OptionalFieldsEmptyOrMissing_ReachApplication
        // Lo que el controlador no valida (modelo, fabricante, mac, mensajes de error)
        // tiene que poder llegar vacio o no llegar, como en el python.
        [Theory]
        [InlineData("PATCH", "/api/roomba/patch-roomba-state", "{\"phase\":\"charge\",\"errorMessage\":\"\",\"pmapId\":\"\",\"userPmapvId\":\"\",\"target\":\"\"}")]
        [InlineData("PATCH", "/api/roomba/patch-roomba-state", "{\"phase\":\"charge\"}")]
        [InlineData("POST", "/api/sensor/presence-sensor/create-presence-sensor", "{\"callout\":\"salon\",\"deviceName\":\"p\",\"deviceType\":\"presence_sensor\",\"model\":\"\",\"manufacturer\":\"\",\"macAddress\":\"\",\"motion\":\"\"}")]
        [InlineData("POST", "/api/sensor/presence-sensor/create-presence-sensor", "{\"callout\":\"salon\",\"deviceName\":\"p\",\"deviceType\":\"presence_sensor\"}")]
        [InlineData("POST", "/api/roomba/create-roomba", "{\"callout\":\"salon\",\"deviceName\":\"r\",\"deviceType\":\"roomba\"}")]
        [InlineData("PATCH", "/api/sensors/rain-sensor/patch-rain-sensor", "{\"callOut\":\"t\",\"deviceName\":\"l\",\"deviceType\":\"rain_sensor\",\"model\":\"\",\"macAddress\":\"\"}")]
        [InlineData("PATCH", "/api/sensors/temperature-sensor/patch-temperature-sensor", "{\"callOut\":\"c\",\"deviceName\":\"t\",\"deviceType\":\"temperature_sensor\"}")]
        public async Task OptionalFieldsEmptyOrMissing_ReachApplication(string method, string url, string body)
        {
            Factory.RoombaApplication.Setup(application => application.PatchRoombaState(It.IsAny<PatchRoombaStateRequest>()))
                .ReturnsAsync(new PatchRoombaStateResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true });
            Factory.RoombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()))
                .ReturnsAsync(new CreateRoombaResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true });
            Factory.PresenceSensorApplication.Setup(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()))
                .ReturnsAsync(new CreatePresenceSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true });
            Factory.RainSensorApplication.Setup(application => application.PatchRainSensor(It.IsAny<PatchRainSensorRequest>()))
                .ReturnsAsync(new PatchRainSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true });
            Factory.TemperatureSensorApplication.Setup(application => application.PatchTemperatureSensor(It.IsAny<PatchTemperatureSensorRequest>()))
                .ReturnsAsync(new PatchTemperatureSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true });

            HttpResponseMessage response = await Send(new HttpMethod(method), url, body);

            Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
        #endregion

        #region MissingRequiredField_Returns400WithoutCallingApplication
        [Fact]
        public async Task MissingRequiredField_Returns400WithoutCallingApplication()
        {
            string body = _presenceBody.Replace("\"callout\":\"salon\",", string.Empty);

            HttpResponseMessage response = await Send(HttpMethod.Post, "/api/sensor/presence-sensor/create-presence-sensor", body);

            await _assertBaseResponse(response, HttpStatusCode.BadRequest, 999, false, "the callout, device name or device type is invalid");
            Factory.PresenceSensorApplication.Verify(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()), Times.Never);
        }
        #endregion
    }
}
