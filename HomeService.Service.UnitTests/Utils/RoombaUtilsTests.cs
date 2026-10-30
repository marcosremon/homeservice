using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Transversal.Common.Utils;

namespace HomeService.Service.UnitTests.Utils
{
    // Solo lo que no abre conexion MQTT: el resto habla con la roomba de verdad.
    public class RoombaUtilsTests
    {
        #region BuildActivationRequest_Defaults
        [Fact]
        public void BuildActivationRequest_Defaults()
        {
            PatchRoombaStateRequest request = RoombaUtils.BuildActivationRequest(RoombaPhase.CHARGE);

            Assert.Equal(RoombaPhase.CHARGE, request.Phase);
            Assert.Equal(RoombaTarget.FULL_HOUSE, request.Target);
            Assert.False(request.IsActivation);
            Assert.False(request.IsFinished);
            Assert.Equal(0, request.BatteryPercent);
            Assert.False(request.BinFull);
            Assert.Equal(0, request.ErrorCode);
            Assert.Equal(string.Empty, request.ErrorMessage);
            Assert.Equal(string.Empty, request.PmapId);
            Assert.Equal(string.Empty, request.UserPmapvId);
            Assert.True(request.IsOnline);
        }
        #endregion

        #region BuildActivationRequest_CopiesEveryArgument
        [Fact]
        public void BuildActivationRequest_CopiesEveryArgument()
        {
            PatchRoombaStateRequest request = RoombaUtils.BuildActivationRequest(
                RoombaPhase.RUN,
                RoombaTarget.BEDROOMS,
                isActivation: true,
                isFinished: true,
                batteryPercent: 77,
                binFull: true,
                errorCode: 15,
                errorMessage: "El roomba reporto error: codigo 15",
                pmapId: "pmap",
                userPmapvId: "version");

            Assert.Equal(RoombaPhase.RUN, request.Phase);
            Assert.Equal(RoombaTarget.BEDROOMS, request.Target);
            Assert.True(request.IsActivation);
            Assert.True(request.IsFinished);
            Assert.Equal(77, request.BatteryPercent);
            Assert.True(request.BinFull);
            Assert.Equal(15, request.ErrorCode);
            Assert.Equal("El roomba reporto error: codigo 15", request.ErrorMessage);
            Assert.Equal("pmap", request.PmapId);
            Assert.Equal("version", request.UserPmapvId);
            Assert.True(request.IsOnline);
        }
        #endregion
    }
}
