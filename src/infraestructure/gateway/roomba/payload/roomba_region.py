from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from infraestructure.gateway.roomba.payload.roomba_region_params import RoombaRegionParams

@dataclass(config = JSON_MODEL_CONFIG)
class RoombaRegion:
    region_id: str | None = Field(default = None, validation_alias = "region_id", serialization_alias = "region_id")
    type: str = "rid"
    params: RoombaRegionParams | None = None