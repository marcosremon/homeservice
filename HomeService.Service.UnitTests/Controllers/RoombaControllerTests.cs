using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Interface.Application;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Service.WebApi.Controllers;
using HomeService.Transversal.JsonInterchange.Roomba.CreateRoomba;
using HomeService.Transversal.JsonInterchange.Roomba.PatchRoombaState;
using HomeService.Transversal.Wrapper.Base;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HomeService.Service.UnitTests.Controllers
{
    public class RoombaControllerTests
    {
        private readonly Mock<IRoombaApplication> _roombaApplication = new Mock<IRoombaApplication>();
        private readonly RoombaController _controller;

        public RoombaControllerTests()
        {
            _controller = new RoombaController(_roombaApplication.Object);
        }

        #region _validCreateRequest
        private static CreateRoombaRequestJson _validCreateRequest() => new CreateRoombaRequestJson
        {
            CallOut = "salon",
            DeviceName = "roomba",
            DeviceType = "roomba",
            Model = "i7",
            Manufacturer = "iRobot",
            MacAddress = "50:14:79:00:00:01",
        };
        #endregion

        #region _capturePatch
        private Func<PatchRoombaStateRequest?> _capturePatch(ResponseCodes responseCode = ResponseCodes.OK)
        {
            PatchRoombaStateRequest? captured = null;
            _roombaApplication.Setup(application => application.PatchRoombaState(It.IsAny<PatchRoombaStateRequest>()))
                .Callback<PatchRoombaStateRequest>(request => captured = request)
                .ReturnsAsync(new PatchRoombaStateResponse { ResponseCode = responseCode, IsSuccess = responseCode == ResponseCodes.OK, Message = "ok" });

            return () => captured;
        }
        #endregion

        #region CreateRoomba_ValidRequest_MapsEveryFieldAndReturns201
        [Fact]
        public async Task CreateRoomba_ValidRequest_MapsEveryFieldAndReturns201()
        {
            CreateRoombaRequest? captured = null;
            _roombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()))
                .Callback<CreateRoombaRequest>(request => captured = request)
                .ReturnsAsync(new CreateRoombaResponse { ResponseCode = ResponseCodes.CREATED, IsSuccess = true, Message = "roomba created" });

            CreateRoombaRequestJson requestJson = _validCreateRequest();
            IActionResult actionResult = await _controller.CreateRoomba(requestJson);

            CreateRoombaResponseJson responseJson = ResultAssert.Json<CreateRoombaResponseJson>(actionResult, 201);
            Assert.Equal("roomba created", responseJson.Message);
            Assert.NotNull(captured);
            Assert.Equal(requestJson.CallOut, captured.CallOut);
            Assert.Equal(requestJson.DeviceName, captured.DeviceName);
            Assert.Equal(requestJson.DeviceType, captured.DeviceType);
            Assert.Equal(requestJson.Model, captured.Model);
            Assert.Equal(requestJson.Manufacturer, captured.Manufacturer);
            Assert.Equal(requestJson.MacAddress, captured.MacAddress);
        }
        #endregion

        #region CreateRoomba_AlreadyExists_Returns409
        // ANY_ROOMBA_EXIST (994) se traduce a 409 en ControllerUtils.
        [Fact]
        public async Task CreateRoomba_AlreadyExists_Returns409()
        {
            _roombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()))
                .ReturnsAsync(new CreateRoombaResponse { ResponseCode = ResponseCodes.ANY_ROOMBA_EXIST, IsSuccess = false, Message = "Any roomba exists on bbdd" });

            IActionResult actionResult = await _controller.CreateRoomba(_validCreateRequest());

            CreateRoombaResponseJson responseJson = ResultAssert.Json<CreateRoombaResponseJson>(actionResult, 409);
            Assert.Equal(ResponseCodesJson.ANY_ROOMBA_EXIST, responseJson.ResponseCodeJson);
        }
        #endregion

        #region CreateRoomba_MissingRequiredField_Returns400
        [Theory]
        [InlineData("callout")]
        [InlineData("deviceName")]
        [InlineData("deviceType")]
        public async Task CreateRoomba_MissingRequiredField_Returns400(string field)
        {
            CreateRoombaRequestJson requestJson = _validCreateRequest();
            if (field == "callout") requestJson.CallOut = string.Empty;
            if (field == "deviceName") requestJson.DeviceName = string.Empty;
            if (field == "deviceType") requestJson.DeviceType = string.Empty;

            IActionResult actionResult = await _controller.CreateRoomba(requestJson);

            CreateRoombaResponseJson responseJson = ResultAssert.Json<CreateRoombaResponseJson>(actionResult, 400);
            Assert.Equal(ResponseCodesJson.INVALID_DATA, responseJson.ResponseCodeJson);
            _roombaApplication.Verify(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>()), Times.Never);
        }
        #endregion

        #region CreateRoomba_Throws_Returns500
        [Fact]
        public async Task CreateRoomba_Throws_Returns500()
        {
            _roombaApplication.Setup(application => application.CreateRoomba(It.IsAny<CreateRoombaRequest>())).ThrowsAsync(new Exception("z"));

            IActionResult actionResult = await _controller.CreateRoomba(_validCreateRequest());

            CreateRoombaResponseJson responseJson = ResultAssert.Json<CreateRoombaResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al crear el roomba z.", responseJson.Message);
        }
        #endregion

        #region PatchRoombaState_ValidRequest_MapsEveryField
        [Fact]
        public async Task PatchRoombaState_ValidRequest_MapsEveryField()
        {
            Func<PatchRoombaStateRequest?> captured = _capturePatch();

            PatchRoombaStateRequestJson requestJson = new PatchRoombaStateRequestJson
            {
                IsActivation = true,
                IsFinished = false,
                Target = "KITCHEN",
                Phase = "RUN",
                BatteryPercent = 87,
                BinFull = true,
                ErrorCode = 3,
                ErrorMessage = "rueda",
                PmapId = "pmap-1",
                UserPmapvId = "v-2",
                IsOnline = true,
            };

            IActionResult actionResult = await _controller.PatchRoombaState(requestJson);

            ResultAssert.Json<PatchRoombaStateResponseJson>(actionResult, 200);
            PatchRoombaStateRequest? request = captured();
            Assert.NotNull(request);
            Assert.True(request.IsActivation);
            Assert.False(request.IsFinished);
            Assert.Equal(RoombaTarget.KITCHEN, request.Target);
            Assert.Equal(RoombaPhase.RUN, request.Phase);
            Assert.Equal(87, request.BatteryPercent);
            Assert.True(request.BinFull);
            Assert.Equal(3, request.ErrorCode);
            Assert.Equal("rueda", request.ErrorMessage);
            Assert.Equal("pmap-1", request.PmapId);
            Assert.Equal("v-2", request.UserPmapvId);
            Assert.True(request.IsOnline);
        }
        #endregion

        #region PatchRoombaState_EnumsAreCaseInsensitiveAndTrimmed
        [Theory]
        [InlineData("kitchen", "run", RoombaTarget.KITCHEN, RoombaPhase.RUN)]
        [InlineData("  Living_Room ", " Charge", RoombaTarget.LIVING_ROOM, RoombaPhase.CHARGE)]
        [InlineData("HALLWAY_AND_TOILET", "hm_usr_dock", RoombaTarget.HALLWAY_AND_TOILET, RoombaPhase.HM_USR_DOCK)]
        public async Task PatchRoombaState_EnumsAreCaseInsensitiveAndTrimmed(string target, string phase, RoombaTarget expectedTarget, RoombaPhase expectedPhase)
        {
            Func<PatchRoombaStateRequest?> captured = _capturePatch();

            await _controller.PatchRoombaState(new PatchRoombaStateRequestJson { Target = target, Phase = phase });

            Assert.Equal(expectedTarget, captured()!.Target);
            Assert.Equal(expectedPhase, captured()!.Phase);
        }
        #endregion

        #region PatchRoombaState_UnknownEnums_FallBackToFullHouseAndStop
        [Theory]
        [InlineData("", "")]
        [InlineData("garaje", "volando")]
        [InlineData("hmUsrDock", "hmUsrDock")]
        public async Task PatchRoombaState_UnknownEnums_FallBackToFullHouseAndStop(string target, string phase)
        {
            Func<PatchRoombaStateRequest?> captured = _capturePatch();

            await _controller.PatchRoombaState(new PatchRoombaStateRequestJson { Target = target, Phase = phase });

            Assert.Equal(RoombaTarget.FULL_HOUSE, captured()!.Target);
            Assert.Equal(RoombaPhase.STOP, captured()!.Phase);
        }
        #endregion

        #region PatchRoombaState_NotFound_Returns404
        [Fact]
        public async Task PatchRoombaState_NotFound_Returns404()
        {
            _capturePatch(ResponseCodes.NOT_FOUND);

            IActionResult actionResult = await _controller.PatchRoombaState(new PatchRoombaStateRequestJson());

            ResultAssert.Json<PatchRoombaStateResponseJson>(actionResult, 404);
        }
        #endregion

        #region PatchRoombaState_Throws_Returns500
        [Fact]
        public async Task PatchRoombaState_Throws_Returns500()
        {
            _roombaApplication.Setup(application => application.PatchRoombaState(It.IsAny<PatchRoombaStateRequest>())).ThrowsAsync(new Exception("w"));

            IActionResult actionResult = await _controller.PatchRoombaState(new PatchRoombaStateRequestJson());

            PatchRoombaStateResponseJson responseJson = ResultAssert.Json<PatchRoombaStateResponseJson>(actionResult, 500);
            Assert.Equal("Ha ocurrido un error al actualizar el estado del roomba w.", responseJson.Message);
        }
        #endregion
    }
}
