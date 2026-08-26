from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaUser:
    user_id: str = Field(default = "", validation_alias = "userId", serialization_alias = "userId")
    access_token: str | None = Field(default = None, validation_alias = "accessToken", serialization_alias = "accessToken")