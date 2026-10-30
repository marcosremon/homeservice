using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.Interface.Application;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Service.WebApi.Controllers;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Transversal.Wrapper.Base;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HomeService.Service.UnitTests.Controllers
{
    public class RainSensorControllerTests
    {
        private readonly Mock<IRainSensorApplication> _rainSensorApplication = new Mock<IRainSensorApplication>();
        private readonly RainSensorController _controller;

        public RainSensorControllerTests()
        {
            _controller = new RainSensorController(_rainSensorApplication.Object);
        }

        #region _validCreateRequest
        private static CreateRainSensorRequestJson _validCreateRequest() => new CreateRainSensorRequestJson
        {
            CallOut = "terraza",
            DeviceName = "lluvia-terraza",
            DeviceType = "rain_sensor",
            Model = "YL-83",
            Manufacturer = "Generic",
            MacAddress = "AA:BB:CC:00:11:22",
            AdcValue = 1800,
            WetnessPercent = 55,
            IsRaining = true,
        };
        #endregion

        #region _validPatchRequest
        private static PatchRainSensorRequestJson _validPatchRequest() => new PatchRainSensorRequestJson
        {
            CallOut = "terraza",
            DeviceName = "lluvia-terraza",
            DeviceType = "rain_sensor",
            Model = "YL-83",
            MacAddress = "AA:BB:CC:00:11:22",
            AdcValue = 1200,
            WetnessPercent = 70,
            IsRaining = true,
        };
        #endregion

        #region CreateRainSensor_ValidRequest_MapsEveryFieldAndReturns201
        [Fact]
        public async Task CreateRainSensor_ValidRequest_MapsEveryFieldAndReturns201()
        {
            CreateRainSensorRequest? captured = null;
            _rainSensorApplication.Setup(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>()))
                .Callback<CreateRainSensorRequest>(request => captured = request)
                .ReturnsAsync(new CreateRainSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "created" });

            CreateRainSensorRequestJson requestJson = _validCreateRequest();
            IActionResult actionResult = await _controller.CreateRainSensor(requestJson);

            ResultAssert.Json<CreateRainSensorResponseJson>(actionResult, 201);
            Assert.NotNull(captured);
            Assert.Equal(requestJson.CallOut, captured.CallOut);
            Assert.Equal(requestJson.DeviceName, captured.DeviceName);
            Assert.Equal(requestJson.DeviceType, captured.DeviceType);
            Assert.Equal(requestJson.Model, captured.Model);
            Assert.Equal(requestJson.Manufacturer, captured.Manufacturer);
            Assert.Equal(requestJson.MacAddress, captured.MacAddress);
            Assert.Equal(requestJson.AdcValue, captured.AdcValue);
            Assert.Equal(requestJson.WetnessPercent, captured.WetnessPercent);
            Assert.Equal(requestJson.IsRaining, captured.IsRaining);
        }
        #endregion

        #region CreateRainSensor_MissingRequiredField_Returns400
        // A diferencia de presencia y roomba, aqui modelo y fabricante tambien son
        // obligatorios.
        [Theory]
        [InlineData("callOut")]
        [InlineData("deviceName")]
        [InlineData("deviceType")]
        [InlineData("model")]
        [InlineData("manufacturer")]
        public async Task CreateRainSensor_MissingRequiredField_Returns400(string field)
        {
            CreateRainSensorRequestJson requestJson = _validCreateRequest();
            if (field == "callOut") requestJson.CallOut = string.Empty;
            if (field == "deviceName") requestJson.DeviceName = string.Empty;
            if (field == "deviceType") requestJson.DeviceType = string.Empty;
            if (field == "model") requestJson.Model = string.Empty;
            if (field == "manufacturer") requestJson.Manufacturer = string.Empty;

            IActionResult actionResult = await _controller.CreateRainSensor(requestJson);

            CreateRainSensorResponseJson responseJson = ResultAssert.Json<CreateRainSensorResponseJson>(actionResult, 400);
            Assert.Equal(ResponseCodesJson.INVALID_DATA, responseJson.ResponseCodeJson);
            Assert.Equal("the data is invalid", responseJson.Message);
            _rainSensorApplication.Verify(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>()), Times.Never);
        }
        #endregion

        #region CreateRainSensor_Conflict_Returns409
        [Fact]
        public async Task CreateRainSensor_Conflict_Returns409()
        {
            _rainSensorApplication.Setup(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>()))
                .ReturnsAsync(new CreateRainSensorResponse { ResponseCode = ResponseCodes.CONFLICT, IsSuccess = false, Message = "already exists" });

            IActionResult actionResult = await _controller.CreateRainSensor(_validCreateRequest());

            CreateRainSensorResponseJson responseJson = ResultAssert.Json<CreateRainSensorResponseJson>(actionResult, 409);
            Assert.Equal(ResponseCodesJson.CONFLICT, responseJson.ResponseCodeJson);
        }
        #endregion

        #region CreateRainSensor_Throws_Returns500
        [Fact]
        public async Task CreateRainSensor_Throws_Returns500()
        {
            _rainSensorApplication.Setup(application => application.CreateRainSensor(It.IsAny<CreateRainSensorRequest>())).ThrowsAsync(new Exception("x"));

            IActionResult actionResult = await _controller.CreateRainSensor(_validCreateRequest());

            CreateRainSensorResponseJson responseJson = ResultAssert.Json<CreateRainSensorResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al crear el sensor de lluvia x.", responseJson.Message);
        }
        #endregion

        #region PatchRainSensor_ValidRequest_MapsFieldsAndReturnsRainStarted
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PatchRainSensor_ValidRequest_MapsFieldsAndReturnsRainStarted(bool rainStarted)
        {
            PatchRainSensorRequest? captured = null;
            _rainSensorApplication.Setup(application => application.PatchRainSensor(It.IsAny<PatchRainSensorRequest>()))
                .Callback<PatchRainSensorRequest>(request => captured = request)
                .ReturnsAsync(new PatchRainSensorResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, RainStarted = rainStarted, Message = "updated" });

            PatchRainSensorRequestJson requestJson = _validPatchRequest();
            IActionResult actionResult = await _controller.PatchRainSensor(requestJson);

            PatchRainSensorResponseJson responseJson = ResultAssert.Json<PatchRainSensorResponseJson>(actionResult, 200);
            Assert.Equal(rainStarted, responseJson.RainStarted);

            Assert.NotNull(captured);
            Assert.Equal(requestJson.CallOut, captured.CallOut);
            Assert.Equal(requestJson.DeviceName, captured.DeviceName);
            Assert.Equal(requestJson.DeviceType, captured.DeviceType);
            Assert.Equal(requestJson.Model, captured.Model);
            Assert.Equal(requestJson.MacAddress, captured.MacAddress);
            Assert.Equal(requestJson.AdcValue, captured.AdcValue);
            Assert.Equal(requestJson.WetnessPercent, captured.WetnessPercent);
            Assert.Equal(requestJson.IsRaining, captured.IsRaining);
        }
        #endregion

        #region PatchRainSensor_MissingCallOut_Returns400
        [Fact]
        public async Task PatchRainSensor_MissingCallOut_Returns400()
        {
            PatchRainSensorRequestJson requestJson = _validPatchRequest();
            requestJson.CallOut = string.Empty;

            IActionResult actionResult = await _controller.PatchRainSensor(requestJson);

            PatchRainSensorResponseJson responseJson = ResultAssert.Json<PatchRainSensorResponseJson>(actionResult, 400);
            Assert.Equal("the callOut, device name or device type is invalid", responseJson.Message);
            Assert.False(responseJson.RainStarted);
        }
        #endregion

        #region PatchRainSensor_Throws_Returns500AndRainStartedFalse
        [Fact]
        public async Task PatchRainSensor_Throws_Returns500AndRainStartedFalse()
        {
            _rainSensorApplication.Setup(application => application.PatchRainSensor(It.IsAny<PatchRainSensorRequest>())).ThrowsAsync(new Exception("y"));

            IActionResult actionResult = await _controller.PatchRainSensor(_validPatchRequest());

            PatchRainSensorResponseJson responseJson = ResultAssert.Json<PatchRainSensorResponseJson>(actionResult, 500);
            Assert.False(responseJson.RainStarted);
            Assert.Equal("Ha ocurrido un error al modificar los datos del sensor de lluvia y.", responseJson.Message);
        }
        #endregion
    }
}
