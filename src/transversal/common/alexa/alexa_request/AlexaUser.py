from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaUser:
    userId: str = Field(default = "", validation_alias = "userId", serialization_alias = "userId")
    accessToken: str | None = Field(default = None, validation_alias = "accessToken", serialization_alias = "accessToken")