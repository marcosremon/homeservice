using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Transversal.Common.Utils;

public static class ControllerUtils
{
    #region HttpResults
    public static IActionResult HttpResults(ResponseCodesJson responseCodesJson, object? content = null) => responseCodesJson switch
    {
        ResponseCodesJson.OK => Json(StatusCodes.Status200OK, content),
        ResponseCodesJson.CREATED => Json(StatusCodes.Status201Created, content),
        ResponseCodesJson.ACCEPTED => Json(StatusCodes.Status202Accepted, content),
        ResponseCodesJson.NO_CONTENT => new StatusCodeResult(StatusCodes.Status204NoContent),
        ResponseCodesJson.BAD_REQUEST => Json(StatusCodes.Status400BadRequest, content),
        ResponseCodesJson.UNAUTHORIZED => Json(StatusCodes.Status401Unauthorized, content),
        ResponseCodesJson.FORBIDDEN => Json(StatusCodes.Status403Forbidden, content),
        ResponseCodesJson.NOT_FOUND => Json(StatusCodes.Status404NotFound, content),
        ResponseCodesJson.METHOD_NOT_ALLOWED => Json(StatusCodes.Status405MethodNotAllowed, content),
        ResponseCodesJson.CONFLICT => Json(StatusCodes.Status409Conflict, content),
        ResponseCodesJson.UNSUPPORTED_MEDIA_TYPE => Json(StatusCodes.Status415UnsupportedMediaType, content),
        ResponseCodesJson.INTERNAL_SERVER_ERROR => Json(StatusCodes.Status500InternalServerError, content),
        ResponseCodesJson.GATEWAY_TIMEOUT => Json(StatusCodes.Status504GatewayTimeout, content),

        ResponseCodesJson.ANY_ROOMBA_EXIST => Json(StatusCodes.Status409Conflict, content),
        ResponseCodesJson.ERROR_READING_FILE => Json(StatusCodes.Status500InternalServerError, content),
        ResponseCodesJson.MOVEMENT_NOT_FOUND => Json(StatusCodes.Status404NotFound, content),
        ResponseCodesJson.ERROR_EMAIL => Json(StatusCodes.Status400BadRequest, content),
        ResponseCodesJson.UNEXPECTED_ERROR => Json(StatusCodes.Status500InternalServerError, content),
        ResponseCodesJson.INVALID_DATA => Json(StatusCodes.Status400BadRequest, content),
        ResponseCodesJson.TOKEN_NO_VALID => Json(StatusCodes.Status401Unauthorized, content),

        _ => Json(StatusCodes.Status500InternalServerError, content)
    };
    #endregion

    #region Json
    private static IActionResult Json(int statusCode, object? content) =>
        new JsonResult(content, JsonModelConfig.Default) { StatusCode = statusCode };
    #endregion
}
