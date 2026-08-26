from pydantic.dataclasses import dataclass

from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson

@dataclass
class BaseResponseJson:
    responseCodeJson: ResponseCodesJson = ResponseCodesJson.UNEXPECTED_ERROR
    isSuccess: bool = False
    message: str = ""