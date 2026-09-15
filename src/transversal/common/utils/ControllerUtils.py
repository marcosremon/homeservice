from typing import Any
from fastapi import status
from fastapi.encoders import jsonable_encoder
from fastapi.responses import JSONResponse, Response
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson

class ControllerUtils:

    #region HttpResults
    @staticmethod
    def HttpResults(responseCodesJson: ResponseCodesJson, content: Any = None) -> Response:
        match responseCodesJson:
            case ResponseCodesJson.OK: return ControllerUtils._json(status.HTTP_200_OK, content)
            case ResponseCodesJson.CREATED: return ControllerUtils._json(status.HTTP_201_CREATED, content)
            case ResponseCodesJson.ACCEPTED: return ControllerUtils._json(status.HTTP_202_ACCEPTED, content)
            case ResponseCodesJson.NO_CONTENT: return Response(status_code = status.HTTP_204_NO_CONTENT)
            case ResponseCodesJson.BAD_REQUEST: return ControllerUtils._json(status.HTTP_400_BAD_REQUEST, content)
            case ResponseCodesJson.UNAUTHORIZED: return ControllerUtils._json(status.HTTP_401_UNAUTHORIZED, content)
            case ResponseCodesJson.FORBIDDEN: return ControllerUtils._json(status.HTTP_403_FORBIDDEN, content)
            case ResponseCodesJson.NOT_FOUND: return ControllerUtils._json(status.HTTP_404_NOT_FOUND, content)
            case ResponseCodesJson.METHOD_NOT_ALLOWED: return ControllerUtils._json(status.HTTP_405_METHOD_NOT_ALLOWED, content)
            case ResponseCodesJson.CONFLICT: return ControllerUtils._json(status.HTTP_409_CONFLICT, content)
            case ResponseCodesJson.UNSUPPORTED_MEDIA_TYPE: return ControllerUtils._json(status.HTTP_415_UNSUPPORTED_MEDIA_TYPE, content)
            case ResponseCodesJson.INTERNAL_SERVER_ERROR: return ControllerUtils._json(status.HTTP_500_INTERNAL_SERVER_ERROR, content)
            case ResponseCodesJson.GATEWAY_TIMEOUT: return ControllerUtils._json(status.HTTP_504_GATEWAY_TIMEOUT, content)

            case ResponseCodesJson.ANY_ROOMBA_EXIST: return ControllerUtils._json(status.HTTP_409_CONFLICT, content)
            case ResponseCodesJson.ERROR_READING_FILE: return ControllerUtils._json(status.HTTP_500_INTERNAL_SERVER_ERROR, content)
            case ResponseCodesJson.MOVEMENT_NOT_FOUND: return ControllerUtils._json(status.HTTP_404_NOT_FOUND, content)
            case ResponseCodesJson.ERROR_EMAIL: return ControllerUtils._json(status.HTTP_400_BAD_REQUEST, content)
            case ResponseCodesJson.UNEXPECTED_ERROR: return ControllerUtils._json(status.HTTP_500_INTERNAL_SERVER_ERROR, content)
            case ResponseCodesJson.INVALID_DATA: return ControllerUtils._json(status.HTTP_400_BAD_REQUEST, content)
            case ResponseCodesJson.TOKEN_NO_VALID: return ControllerUtils._json(status.HTTP_401_UNAUTHORIZED, content)
    #endregion

    #region _json
    @staticmethod
    def _json(statusCode: int, content: Any) -> JSONResponse:
        return JSONResponse(status_code = statusCode, content = jsonable_encoder(content, by_alias = True, exclude_none = True))
    #endregion