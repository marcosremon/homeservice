from dataclasses import dataclass

from transversal.common.wrappers.base.base_response import BaseResponse

@dataclass
class GetComputerStatusResponse(BaseResponse):
    computer_status: bool = False