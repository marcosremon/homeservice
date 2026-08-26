from dataclasses import dataclass, field

from application.data_transfer_object.light.light_dto import LightDto
from transversal.common.wrappers.base.base_response import BaseResponse

@dataclass
class GetLightByLocationResponse(BaseResponse):
    light_dto: LightDto = field(default_factory = LightDto)