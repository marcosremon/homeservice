from dataclasses import dataclass

from transversal.common.wrappers.base.BaseResponse import BaseResponse

@dataclass
class GetComputerStatusResponse(BaseResponse):
    ComputerStatus: bool = False