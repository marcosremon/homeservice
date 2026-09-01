from domain.model.entity.RainSensor import RainSensor
from transversal.common.wrappers.base.BaseResponse import BaseResponse

class GetRainingSensorResponse(BaseResponse):
    rainingSensors: list[RainSensor] = []