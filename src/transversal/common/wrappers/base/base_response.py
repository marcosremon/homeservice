from dataclasses import dataclass

from transversal.common.wrappers.base.response_codes import ResponseCodes

@dataclass
class BaseResponse:
    response_code: ResponseCodes = ResponseCodes.UNEXPECTED_ERROR
    is_success: bool = False
    message: str = ""