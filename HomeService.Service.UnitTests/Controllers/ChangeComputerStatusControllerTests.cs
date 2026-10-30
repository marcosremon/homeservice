using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.Interface.Application;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Service.WebApi.Controllers;
using HomeService.Transversal.JsonInterchange.ChangeComputerStatus.GetComputerStatus;
using HomeService.Transversal.JsonInterchange.ChangeComputerStatus.TurnOffComputer;
using HomeService.Transversal.JsonInterchange.ChangeComputerStatus.TurnOnComputer;
using HomeService.Transversal.Wrapper.Base;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HomeService.Service.UnitTests.Controllers
{
    public class ChangeComputerStatusControllerTests
    {
        private readonly Mock<IChangeComputerStatusApplication> _changeComputerStatusApplication = new Mock<IChangeComputerStatusApplication>();
        private readonly ChangeComputerStatusController _controller;

        public ChangeComputerStatusControllerTests()
        {
            _controller = new ChangeComputerStatusController(_changeComputerStatusApplication.Object);
        }

        #region TurnOnComputer_Success_Returns200WithApplicationMessage
        [Fact]
        public async Task TurnOnComputer_Success_Returns200WithApplicationMessage()
        {
            _changeComputerStatusApplication.Setup(application => application.TurnOnComputer())
                .ReturnsAsync(new TurnOnComputerResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "WoL sent. PC booting into CachyOS." });

            IActionResult actionResult = await _controller.TurnOnComputer();

            TurnOnComputerResponseJson responseJson = ResultAssert.Json<TurnOnComputerResponseJson>(actionResult, 200);
            Assert.Equal(ResponseCodesJson.OK, responseJson.ResponseCodeJson);
            Assert.True(responseJson.IsSuccess);
            Assert.Equal("WoL sent. PC booting into CachyOS.", responseJson.Message);
        }
        #endregion

        #region TurnOnComputer_ApplicationFails_PropagatesResponseCode
        [Fact]
        public async Task TurnOnComputer_ApplicationFails_PropagatesResponseCode()
        {
            _changeComputerStatusApplication.Setup(application => application.TurnOnComputer())
                .ReturnsAsync(new TurnOnComputerResponse { ResponseCode = ResponseCodes.UNEXPECTED_ERROR, IsSuccess = false, Message = "Missing LanParameters" });

            IActionResult actionResult = await _controller.TurnOnComputer();

            TurnOnComputerResponseJson responseJson = ResultAssert.Json<TurnOnComputerResponseJson>(actionResult, 500);
            Assert.Equal(ResponseCodesJson.UNEXPECTED_ERROR, responseJson.ResponseCodeJson);
            Assert.False(responseJson.IsSuccess);
        }
        #endregion

        #region TurnOnComputer_Throws_Returns500WithOnlyExceptionMessage
        [Fact]
        public async Task TurnOnComputer_Throws_Returns500WithOnlyExceptionMessage()
        {
            _changeComputerStatusApplication.Setup(application => application.TurnOnComputer()).ThrowsAsync(new InvalidOperationException("boom"));

            IActionResult actionResult = await _controller.TurnOnComputer();

            TurnOnComputerResponseJson responseJson = ResultAssert.Json<TurnOnComputerResponseJson>(actionResult, 500);
            Assert.Equal(ResponseCodesJson.INTERNAL_SERVER_ERROR, responseJson.ResponseCodeJson);
            Assert.False(responseJson.IsSuccess);
            Assert.Equal("Ha ocurrido un error al encender el ordenador boom.", responseJson.Message);
            Assert.DoesNotContain("InvalidOperationException", responseJson.Message);
        }
        #endregion

        #region TurnOffComputer_Success_Returns200
        [Fact]
        public async Task TurnOffComputer_Success_Returns200()
        {
            _changeComputerStatusApplication.Setup(application => application.TurnOffComputer())
                .ReturnsAsync(new TurnOffComputerResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, Message = "El ordenador ya esta apagado." });

            IActionResult actionResult = await _controller.TurnOffComputer();

            TurnOffComputerResponseJson responseJson = ResultAssert.Json<TurnOffComputerResponseJson>(actionResult, 200);
            Assert.True(responseJson.IsSuccess);
            Assert.Equal("El ordenador ya esta apagado.", responseJson.Message);
        }
        #endregion

        #region TurnOffComputer_Throws_Returns500
        [Fact]
        public async Task TurnOffComputer_Throws_Returns500()
        {
            _changeComputerStatusApplication.Setup(application => application.TurnOffComputer()).ThrowsAsync(new Exception("ssh caido"));

            IActionResult actionResult = await _controller.TurnOffComputer();

            TurnOffComputerResponseJson responseJson = ResultAssert.Json<TurnOffComputerResponseJson>(actionResult, 500);
            Assert.Equal(ResponseCodesJson.INTERNAL_SERVER_ERROR, responseJson.ResponseCodeJson);
            Assert.Equal("Ha ocurrido un error al apagar el ordenador ssh caido.", responseJson.Message);
        }
        #endregion

        #region GetComputerStatus_Success_ReturnsComputerStatus
        [Theory]
        [InlineData(true, "ON")]
        [InlineData(false, "OFF")]
        public async Task GetComputerStatus_Success_ReturnsComputerStatus(bool computerStatus, string message)
        {
            _changeComputerStatusApplication.Setup(application => application.GetComputerStatus())
                .ReturnsAsync(new GetComputerStatusResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, ComputerStatus = computerStatus, Message = message });

            IActionResult actionResult = await _controller.GetComputerStatus();

            GetComputerStatusResponseJson responseJson = ResultAssert.Json<GetComputerStatusResponseJson>(actionResult, 200);
            Assert.Equal(computerStatus, responseJson.ComputerStatus);
            Assert.Equal(message, responseJson.Message);
        }
        #endregion

        #region GetComputerStatus_Throws_Returns500AndStatusFalse
        [Fact]
        public async Task GetComputerStatus_Throws_Returns500AndStatusFalse()
        {
            _changeComputerStatusApplication.Setup(application => application.GetComputerStatus()).ThrowsAsync(new Exception("ping"));

            IActionResult actionResult = await _controller.GetComputerStatus();

            GetComputerStatusResponseJson responseJson = ResultAssert.Json<GetComputerStatusResponseJson>(actionResult, 500);
            Assert.False(responseJson.ComputerStatus);
            Assert.False(responseJson.IsSuccess);
            Assert.Equal("Ha ocurrido un error al consultar el estado del ordenador ping.", responseJson.Message);
        }
        #endregion
    }
}
