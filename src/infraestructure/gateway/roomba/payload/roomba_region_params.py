from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class RoombaRegionParams:
    no_auto_passes: bool = Field(default = False, validation_alias = "noAutoPasses", serialization_alias = "noAutoPasses")
    two_pass: bool = Field(default = False, validation_alias = "twoPass", serialization_alias = "twoPass")