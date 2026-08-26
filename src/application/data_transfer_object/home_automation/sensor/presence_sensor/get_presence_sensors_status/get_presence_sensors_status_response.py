from dataclasses import dataclass
from datetime import datetime

from transversal.common.wrappers.base.base_response import BaseResponse

@dataclass
class GetPresenceSensorsStatusResponse(BaseResponse):
    is_house_empty: bool = False
    last_roomba_activation: datetime = datetime.min