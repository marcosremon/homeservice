from pydantic.dataclasses import dataclass

from transversal.common.wrappers.json.BaseResponseJson import BaseResponseJson

@dataclass
class GetComputerStatusResponseJson(BaseResponseJson):
    ComputerStatus: bool = False