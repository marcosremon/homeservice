from dataclasses import dataclass

from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

@dataclass
class BaseResponse:
    responseCode: ResponseCodes = ResponseCodes.UNEXPECTED_ERROR
    isSuccess: bool = False
    message: str = ""