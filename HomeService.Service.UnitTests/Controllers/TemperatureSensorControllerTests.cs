using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.Interface.Application;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Service.WebApi.Controllers;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Transversal.Wrapper.Base;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HomeService.Service.UnitTests.Controllers
{
    public class TemperatureSensorControllerTests
    {
        private readonly Mock<ITemperatureSensorApplication> _temperatureSensorApplication = new Mock<ITemperatureSensorApplication>();
        private readonly TemperatureSensorController _controller;

        public TemperatureSensorControllerTests()
        {
            _controller = new TemperatureSensorController(_temperatureSensorApplication.Object);
        }

        #region _validCreateRequest
        private static CreateTemperatureSensorRequestJson _validCreateRequest() => new CreateTemperatureSensorRequestJson
        {
            CallOut = "cocina",
            DeviceName = "temp-cocina",
            DeviceType = "temperature_sensor",
            Model = "LM35",
            Manufacturer = "TI",
            MacAddress = "AA:00:00:00:00:01",
            Temperature = 21.5,
            AdcVoltage = 0.215,
            MeasureAt = 1730000000,
        };
        #endregion

        #region CreateTemperatureSensor_ValidRequest_MapsEveryFieldAndReturns201
        [Fact]
        public async Task CreateTemperatureSensor_ValidRequest_MapsEveryFieldAndReturns201()
        {
            CreateTemperatureSensorRequest? captured = null;
            _temperatureSensorApplication.Setup(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>()))
                .Callback<CreateTemperatureSensorRequest>(request => captured = request)
                .ReturnsAsync(new CreateTemperatureSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true });

            CreateTemperatureSensorRequestJson requestJson = _validCreateRequest();
            IActionResult actionResult = await _controller.CreateTemperatureSensor(requestJson);

            ResultAssert.Json<CreateTemperatureSensorResponseJson>(actionResult, 201);
            Assert.NotNull(captured);
            Assert.Equal(requestJson.CallOut, captured.CallOut);
            Assert.Equal(requestJson.DeviceName, captured.DeviceName);
            Assert.Equal(requestJson.DeviceType, captured.DeviceType);
            Assert.Equal(requestJson.Model, captured.Model);
            Assert.Equal(requestJson.Manufacturer, captured.Manufacturer);
            Assert.Equal(requestJson.MacAddress, captured.MacAddress);
            Assert.Equal(requestJson.Temperature, captured.Temperature);
            Assert.Equal(requestJson.AdcVoltage, captured.AdcVoltage);
            Assert.Equal(requestJson.MeasureAt, captured.MeasureAt);
        }
        #endregion

        #region CreateTemperatureSensor_InvalidField_Returns400
        // Las lecturas a 0.0 cuentan como "no enviadas": un sensor que mida 0 grados
        // exactos se rechaza.
        [Theory]
        [InlineData("callOut")]
        [InlineData("deviceName")]
        [InlineData("deviceType")]
        [InlineData("model")]
        [InlineData("manufacturer")]
        [InlineData("temperature")]
        [InlineData("adcVoltage")]
        [InlineData("measureAt")]
        public async Task CreateTemperatureSensor_InvalidField_Returns400(string field)
        {
            CreateTemperatureSensorRequestJson requestJson = _validCreateRequest();
            if (field == "callOut") requestJson.CallOut = string.Empty;
            if (field == "deviceName") requestJson.DeviceName = string.Empty;
            if (field == "deviceType") requestJson.DeviceType = string.Empty;
            if (field == "model") requestJson.Model = string.Empty;
            if (field == "manufacturer") requestJson.Manufacturer = string.Empty;
            if (field == "temperature") requestJson.Temperature = 0.0;
            if (field == "adcVoltage") requestJson.AdcVoltage = 0.0;
            if (field == "measureAt") requestJson.MeasureAt = 0.0;

            IActionResult actionResult = await _controller.CreateTemperatureSensor(requestJson);

            CreateTemperatureSensorResponseJson responseJson = ResultAssert.Json<CreateTemperatureSensorResponseJson>(actionResult, 400);
            Assert.Equal(ResponseCodesJson.INVALID_DATA, responseJson.ResponseCodeJson);
            Assert.Equal("the data is invalid", responseJson.Message);
            _temperatureSensorApplication.Verify(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>()), Times.Never);
        }
        #endregion

        #region CreateTemperatureSensor_NegativeTemperature_IsAccepted
        [Fact]
        public async Task CreateTemperatureSensor_NegativeTemperature_IsAccepted()
        {
            _temperatureSensorApplication.Setup(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>()))
                .ReturnsAsync(new CreateTemperatureSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true });

            CreateTemperatureSensorRequestJson requestJson = _validCreateRequest();
            requestJson.Temperature = -3.5;

            IActionResult actionResult = await _controller.CreateTemperatureSensor(requestJson);

            ResultAssert.Json<CreateTemperatureSensorResponseJson>(actionResult, 201);
        }
        #endregion

        #region CreateTemperatureSensor_Conflict_Returns409
        [Fact]
        public async Task CreateTemperatureSensor_Conflict_Returns409()
        {
            _temperatureSensorApplication.Setup(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>()))
                .ReturnsAsync(new CreateTemperatureSensorResponse { ResponseCode = ResponseCodes.CONFLICT, IsSuccess = false });

            IActionResult actionResult = await _controller.CreateTemperatureSensor(_validCreateRequest());

            ResultAssert.Json<CreateTemperatureSensorResponseJson>(actionResult, 409);
        }
        #endregion

        #region CreateTemperatureSensor_Throws_Returns500
        [Fact]
        public async Task CreateTemperatureSensor_Throws_Returns500()
        {
            _temperatureSensorApplication.Setup(application => application.CreateTemperatureSensor(It.IsAny<CreateTemperatureSensorRequest>())).ThrowsAsync(new Exception("t"));

            IActionResult actionResult = await _controller.CreateTemperatureSensor(_validCreateRequest());

            CreateTemperatureSensorResponseJson responseJson = ResultAssert.Json<CreateTemperatureSensorResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al crear el sensor de temperatura t.", responseJson.Message);
        }
        #endregion

        #region PatchTemperatureSensor_ValidRequest_MapsOptionalReadings
        [Theory]
        [InlineData(22.0, 0.22, 1730000005.0)]
        [InlineData(null, null, null)]
        [InlineData(0.0, null, 5.0)]
        public async Task PatchTemperatureSensor_ValidRequest_MapsOptionalReadings(double? temperature, double? adcVoltage, double? measureAt)
        {
            PatchTemperatureSensorRequest? captured = null;
            _temperatureSensorApplication.Setup(application => application.PatchTemperatureSensor(It.IsAny<PatchTemperatureSensorRequest>()))
                .Callback<PatchTemperatureSensorRequest>(request => captured = request)
                .ReturnsAsync(new PatchTemperatureSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true });

            PatchTemperatureSensorRequestJson requestJson = new PatchTemperatureSensorRequestJson
            {
                CallOut = "cocina",
                DeviceName = "temp-cocina",
                DeviceType = "temperature_sensor",
                Model = "LM35",
                MacAddress = "AA:00:00:00:00:01",
                Temperature = temperature,
                AdcVoltage = adcVoltage,
                MeasureAt = measureAt,
            };

            IActionResult actionResult = await _controller.PatchTemperatureSensor(requestJson);

            ResultAssert.Json<PatchTemperatureSensorResponseJson>(actionResult, 200);
            Assert.NotNull(captured);
            Assert.Equal("cocina", captured.CallOut);
            Assert.Equal("temp-cocina", captured.DeviceName);
            Assert.Equal("temperature_sensor", captured.DeviceType);
            Assert.Equal("LM35", captured.Model);
            Assert.Equal("AA:00:00:00:00:01", captured.MacAddress);
            Assert.Equal(temperature, captured.Temperature);
            Assert.Equal(adcVoltage, captured.AdcVoltage);
            Assert.Equal(measureAt, captured.MeasureAt);
        }
        #endregion

        #region PatchTemperatureSensor_MissingDeviceType_Returns400
        [Fact]
        public async Task PatchTemperatureSensor_MissingDeviceType_Returns400()
        {
            PatchTemperatureSensorRequestJson requestJson = new PatchTemperatureSensorRequestJson { CallOut = "cocina", DeviceName = "temp-cocina" };

            IActionResult actionResult = await _controller.PatchTemperatureSensor(requestJson);

            PatchTemperatureSensorResponseJson responseJson = ResultAssert.Json<PatchTemperatureSensorResponseJson>(actionResult, 400);
            Assert.Equal("the callOut, device name or device type is invalid", responseJson.Message);
        }
        #endregion

        #region PatchTemperatureSensor_Throws_Returns500
        [Fact]
        public async Task PatchTemperatureSensor_Throws_Returns500()
        {
            _temperatureSensorApplication.Setup(application => application.PatchTemperatureSensor(It.IsAny<PatchTemperatureSensorRequest>())).ThrowsAsync(new Exception("p"));

            IActionResult actionResult = await _controller.PatchTemperatureSensor(new PatchTemperatureSensorRequestJson { CallOut = "c", DeviceName = "d", DeviceType = "t" });

            PatchTemperatureSensorResponseJson responseJson = ResultAssert.Json<PatchTemperatureSensorResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al modificar los datos del sensor de temperatura p.", responseJson.Message);
        }
        #endregion
    }
}
