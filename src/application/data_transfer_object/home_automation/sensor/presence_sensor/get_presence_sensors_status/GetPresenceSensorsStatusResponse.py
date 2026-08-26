from dataclasses import dataclass
from datetime import datetime

from transversal.common.wrappers.base.BaseResponse import BaseResponse

@dataclass
class GetPresenceSensorsStatusResponse(BaseResponse):
    isHouseEmpty: bool = False
    lastRoombaActivation: datetime = datetime.min