from pydantic.dataclasses import dataclass

from transversal.common.wrappers.json.base_response_json import BaseResponseJson

@dataclass
class GetComputerStatusResponseJson(BaseResponseJson):
    computer_status: bool = False
