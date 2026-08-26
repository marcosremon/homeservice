from dataclasses import dataclass, field

from application.data_transfer_object.light.LightDto import LightDto
from transversal.common.wrappers.base.BaseResponse import BaseResponse

@dataclass
class GetLightByLocationResponse(BaseResponse):
    lightDto: LightDto = field(default_factory = LightDto)