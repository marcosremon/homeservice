from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from infraestructure.gateway.roomba.payload.RoombaRegionParams import RoombaRegionParams

@dataclass(config = JSON_MODEL_CONFIG)
class RoombaRegion:
    regionId: str | None = Field(default = None, validation_alias = "region_id", serialization_alias = "region_id")
    type: str = "rid"
    params: RoombaRegionParams | None = None