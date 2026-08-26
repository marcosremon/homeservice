from pydantic import Field
from pydantic.dataclasses import dataclass

from infraestructure.gateway.roomba.payload.roomba_region_params import RoombaRegionParams

@dataclass
class RoombaRegion:
    region_id: str | None = Field(default = None, alias = "region_id")
    type: str = "rid"
    params: RoombaRegionParams | None = None