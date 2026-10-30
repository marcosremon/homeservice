using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.Interface.Application;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Service.WebApi.Controllers;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Transversal.Wrapper.Base;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HomeService.Service.UnitTests.Controllers
{
    public class PresenceSensorControllerTests
    {
        private readonly Mock<IPresenceSensorApplication> _presenceSensorApplication = new Mock<IPresenceSensorApplication>();
        private readonly PresenceSensorController _controller;

        public PresenceSensorControllerTests()
        {
            _controller = new PresenceSensorController(_presenceSensorApplication.Object);
        }

        #region _validCreateRequest
        private static CreatePresenceSensorRequestJson _validCreateRequest() => new CreatePresenceSensorRequestJson
        {
            CallOut = "salon",
            DeviceName = "presencia-salon",
            DeviceType = "presence_sensor",
            Ts = 1730000000,
            Presence = true,
            DistanceCm = 120,
            Motion = "moving",
            Model = "LD2410",
            Manufacturer = "HLK",
            MacAddress = "AA:BB:CC:DD:EE:FF",
        };
        #endregion

        #region _validPatchRequest
        private static PatchPresenceSensorDataRequestJson _validPatchRequest() => new PatchPresenceSensorDataRequestJson
        {
            CallOut = "salon",
            DeviceName = "presencia-salon",
            DeviceType = "presence_sensor",
            Model = "LD2410",
            Manufacturer = "HLK",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            Ts = 1730000001,
            Presence = false,
            DistanceCm = 0,
            Motion = "none",
        };
        #endregion

        #region CreatePresenceSensor_ValidRequest_MapsEveryFieldAndReturns201
        [Fact]
        public async Task CreatePresenceSensor_ValidRequest_MapsEveryFieldAndReturns201()
        {
            CreatePresenceSensorRequest? captured = null;
            _presenceSensorApplication.Setup(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()))
                .Callback<CreatePresenceSensorRequest>(request => captured = request)
                .ReturnsAsync(new CreatePresenceSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "created" });

            CreatePresenceSensorRequestJson requestJson = _validCreateRequest();
            IActionResult actionResult = await _controller.CreatePresenceSensor(requestJson);

            CreatePresenceSensorResponseJson responseJson = ResultAssert.Json<CreatePresenceSensorResponseJson>(actionResult, 201);
            Assert.Equal(ResponseCodesJson.CREATED, responseJson.ResponseCodeJson);
            Assert.True(responseJson.IsSuccess);

            Assert.NotNull(captured);
            Assert.Equal(requestJson.CallOut, captured.CallOut);
            Assert.Equal(requestJson.DeviceName, captured.DeviceName);
            Assert.Equal(requestJson.DeviceType, captured.DeviceType);
            Assert.Equal(requestJson.Ts, captured.Ts);
            Assert.Equal(requestJson.Presence, captured.Presence);
            Assert.Equal(requestJson.DistanceCm, captured.DistanceCm);
            Assert.Equal(requestJson.Motion, captured.Motion);
            Assert.Equal(requestJson.Model, captured.Model);
            Assert.Equal(requestJson.Manufacturer, captured.Manufacturer);
            Assert.Equal(requestJson.MacAddress, captured.MacAddress);
        }
        #endregion

        #region CreatePresenceSensor_MissingRequiredField_Returns400WithoutCallingApplication
        [Theory]
        [InlineData("callout")]
        [InlineData("deviceName")]
        [InlineData("deviceType")]
        public async Task CreatePresenceSensor_MissingRequiredField_Returns400WithoutCallingApplication(string field)
        {
            CreatePresenceSensorRequestJson requestJson = _validCreateRequest();
            if (field == "callout") requestJson.CallOut = string.Empty;
            if (field == "deviceName") requestJson.DeviceName = string.Empty;
            if (field == "deviceType") requestJson.DeviceType = string.Empty;

            IActionResult actionResult = await _controller.CreatePresenceSensor(requestJson);

            CreatePresenceSensorResponseJson responseJson = ResultAssert.Json<CreatePresenceSensorResponseJson>(actionResult, 400);
            Assert.Equal(ResponseCodesJson.INVALID_DATA, responseJson.ResponseCodeJson);
            Assert.False(responseJson.IsSuccess);
            Assert.Equal("the callout, device name or device type is invalid", responseJson.Message);
            _presenceSensorApplication.Verify(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()), Times.Never);
        }
        #endregion

        #region CreatePresenceSensor_OptionalFieldsEmpty_IsAccepted
        [Fact]
        public async Task CreatePresenceSensor_OptionalFieldsEmpty_IsAccepted()
        {
            _presenceSensorApplication.Setup(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()))
                .ReturnsAsync(new CreatePresenceSensorResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true });

            CreatePresenceSensorRequestJson requestJson = _validCreateRequest();
            requestJson.Model = string.Empty;
            requestJson.Manufacturer = string.Empty;
            requestJson.MacAddress = string.Empty;

            IActionResult actionResult = await _controller.CreatePresenceSensor(requestJson);

            ResultAssert.Json<CreatePresenceSensorResponseJson>(actionResult, 201);
        }
        #endregion

        #region CreatePresenceSensor_Throws_Returns500
        [Fact]
        public async Task CreatePresenceSensor_Throws_Returns500()
        {
            _presenceSensorApplication.Setup(application => application.CreatePresenceSensor(It.IsAny<CreatePresenceSensorRequest>()))
                .ThrowsAsync(new Exception("db down"));

            IActionResult actionResult = await _controller.CreatePresenceSensor(_validCreateRequest());

            CreatePresenceSensorResponseJson responseJson = ResultAssert.Json<CreatePresenceSensorResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al crear el sensor de presencia db down.", responseJson.Message);
        }
        #endregion

        #region PatchPresenceSensorData_ValidRequest_MapsEveryFieldAndReturns200
        [Fact]
        public async Task PatchPresenceSensorData_ValidRequest_MapsEveryFieldAndReturns200()
        {
            PatchPresenceSensorDataRequest? captured = null;
            _presenceSensorApplication.Setup(application => application.PatchPresenceSensorData(It.IsAny<PatchPresenceSensorDataRequest>()))
                .Callback<PatchPresenceSensorDataRequest>(request => captured = request)
                .ReturnsAsync(new PatchPresenceSensorDataResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "updated" });

            PatchPresenceSensorDataRequestJson requestJson = _validPatchRequest();
            IActionResult actionResult = await _controller.PatchPresenceSensorData(requestJson);

            PatchPresenceSensorDataResponseJson responseJson = ResultAssert.Json<PatchPresenceSensorDataResponseJson>(actionResult, 200);
            Assert.Equal("updated", responseJson.Message);

            Assert.NotNull(captured);
            Assert.Equal(requestJson.CallOut, captured.CallOut);
            Assert.Equal(requestJson.DeviceName, captured.DeviceName);
            Assert.Equal(requestJson.DeviceType, captured.DeviceType);
            Assert.Equal(requestJson.Model, captured.Model);
            Assert.Equal(requestJson.Manufacturer, captured.Manufacturer);
            Assert.Equal(requestJson.MacAddress, captured.MacAddress);
            Assert.Equal(requestJson.Ts, captured.Ts);
            Assert.Equal(requestJson.Presence, captured.Presence);
            Assert.Equal(requestJson.DistanceCm, captured.DistanceCm);
            Assert.Equal(requestJson.Motion, captured.Motion);
        }
        #endregion

        #region PatchPresenceSensorData_NotFound_Returns404
        [Fact]
        public async Task PatchPresenceSensorData_NotFound_Returns404()
        {
            _presenceSensorApplication.Setup(application => application.PatchPresenceSensorData(It.IsAny<PatchPresenceSensorDataRequest>()))
                .ReturnsAsync(new PatchPresenceSensorDataResponse { ResponseCode = ResponseCodes.NOT_FOUND, IsSuccess = false, Message = "House zone salon not found" });

            IActionResult actionResult = await _controller.PatchPresenceSensorData(_validPatchRequest());

            PatchPresenceSensorDataResponseJson responseJson = ResultAssert.Json<PatchPresenceSensorDataResponseJson>(actionResult, 404);
            Assert.Equal(ResponseCodesJson.NOT_FOUND, responseJson.ResponseCodeJson);
        }
        #endregion

        #region PatchPresenceSensorData_MissingDeviceName_Returns400
        [Fact]
        public async Task PatchPresenceSensorData_MissingDeviceName_Returns400()
        {
            PatchPresenceSensorDataRequestJson requestJson = _validPatchRequest();
            requestJson.DeviceName = string.Empty;

            IActionResult actionResult = await _controller.PatchPresenceSensorData(requestJson);

            PatchPresenceSensorDataResponseJson responseJson = ResultAssert.Json<PatchPresenceSensorDataResponseJson>(actionResult, 400);
            Assert.Equal(ResponseCodesJson.INVALID_DATA, responseJson.ResponseCodeJson);
            _presenceSensorApplication.Verify(application => application.PatchPresenceSensorData(It.IsAny<PatchPresenceSensorDataRequest>()), Times.Never);
        }
        #endregion

        #region PatchPresenceSensorData_Throws_Returns500
        [Fact]
        public async Task PatchPresenceSensorData_Throws_Returns500()
        {
            _presenceSensorApplication.Setup(application => application.PatchPresenceSensorData(It.IsAny<PatchPresenceSensorDataRequest>()))
                .ThrowsAsync(new Exception("timeout"));

            IActionResult actionResult = await _controller.PatchPresenceSensorData(_validPatchRequest());

            PatchPresenceSensorDataResponseJson responseJson = ResultAssert.Json<PatchPresenceSensorDataResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al modificar los datos del sensor de presencia timeout.", responseJson.Message);
        }
        #endregion
    }
}
