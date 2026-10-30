using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.UnitTests.Utils
{
    public class ControllerUtilsTests
    {
        #region HttpResults_MapsEveryResponseCode
        [Theory]
        [InlineData(ResponseCodesJson.OK, 200)]
        [InlineData(ResponseCodesJson.CREATED, 201)]
        [InlineData(ResponseCodesJson.ACCEPTED, 202)]
        [InlineData(ResponseCodesJson.BAD_REQUEST, 400)]
        [InlineData(ResponseCodesJson.UNAUTHORIZED, 401)]
        [InlineData(ResponseCodesJson.FORBIDDEN, 403)]
        [InlineData(ResponseCodesJson.NOT_FOUND, 404)]
        [InlineData(ResponseCodesJson.METHOD_NOT_ALLOWED, 405)]
        [InlineData(ResponseCodesJson.CONFLICT, 409)]
        [InlineData(ResponseCodesJson.UNSUPPORTED_MEDIA_TYPE, 415)]
        [InlineData(ResponseCodesJson.INTERNAL_SERVER_ERROR, 500)]
        [InlineData(ResponseCodesJson.GATEWAY_TIMEOUT, 504)]
        [InlineData(ResponseCodesJson.ANY_ROOMBA_EXIST, 409)]
        [InlineData(ResponseCodesJson.ERROR_READING_FILE, 500)]
        [InlineData(ResponseCodesJson.MOVEMENT_NOT_FOUND, 404)]
        [InlineData(ResponseCodesJson.ERROR_EMAIL, 400)]
        [InlineData(ResponseCodesJson.UNEXPECTED_ERROR, 500)]
        [InlineData(ResponseCodesJson.INVALID_DATA, 400)]
        [InlineData(ResponseCodesJson.TOKEN_NO_VALID, 401)]
        public void HttpResults_MapsEveryResponseCode(ResponseCodesJson responseCodesJson, int expectedStatusCode)
        {
            object content = new { value = 1 };

            IActionResult actionResult = ControllerUtils.HttpResults(responseCodesJson, content);

            JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
            Assert.Equal(expectedStatusCode, jsonResult.StatusCode);
            Assert.Same(content, jsonResult.Value);
            Assert.Same(JsonModelConfig.Default, jsonResult.SerializerSettings);
        }
        #endregion

        #region HttpResults_NoContent_HasNoBody
        [Fact]
        public void HttpResults_NoContent_HasNoBody()
        {
            IActionResult actionResult = ControllerUtils.HttpResults(ResponseCodesJson.NO_CONTENT, new { value = 1 });

            StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
            Assert.Equal(204, statusCodeResult.StatusCode);
        }
        #endregion

        #region HttpResults_UnknownCode_Returns500
        [Fact]
        public void HttpResults_UnknownCode_Returns500()
        {
            JsonResult jsonResult = Assert.IsType<JsonResult>(ControllerUtils.HttpResults((ResponseCodesJson)12345));

            Assert.Equal(500, jsonResult.StatusCode);
        }
        #endregion

        #region ResponseCodes_AndResponseCodesJson_HaveSameValues
        // Los controladores convierten uno en otro con un cast: si alguien anade un
        // codigo solo en un enum, el cast da un valor sin nombre.
        [Fact]
        public void ResponseCodes_AndResponseCodesJson_HaveSameValues()
        {
            foreach (ResponseCodes responseCode in Enum.GetValues<ResponseCodes>())
            {
                ResponseCodesJson responseCodeJson = (ResponseCodesJson)responseCode;

                Assert.True(Enum.IsDefined(responseCodeJson), $"{responseCode} ({(int)responseCode}) no existe en ResponseCodesJson");
                Assert.Equal(responseCode.ToString(), responseCodeJson.ToString());
            }
        }
        #endregion

        #region BaseResponse_DefaultsToUnexpectedError
        [Fact]
        public void BaseResponse_DefaultsToUnexpectedError()
        {
            Assert.Equal(ResponseCodes.UNEXPECTED_ERROR, new BaseResponse().ResponseCode);
            Assert.Equal(ResponseCodesJson.UNEXPECTED_ERROR, new BaseResponseJson().ResponseCodeJson);
        }
        #endregion
    }
}
