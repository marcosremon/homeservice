from transversal.common.wrappers.json.response_codes_json import ResponseCodesJson

class BaseResponseJson:
    response_code_json: ResponseCodesJson = ResponseCodesJson.UNEXPECTED_ERROR
    is_success: bool = False
    message: str = ""
