from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class RoombaRegionParams:
    noAutoPasses: bool = Field(default = False, validation_alias = "noAutoPasses", serialization_alias = "noAutoPasses")
    twoPass: bool = Field(default = False, validation_alias = "twoPass", serialization_alias = "twoPass")