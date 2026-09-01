from dataclasses import dataclass
from transversal.common.wrappers.base.BaseResponse import BaseResponse

@dataclass
class PatchRainSensorResponse(BaseResponse):
    rainStarted: bool = False