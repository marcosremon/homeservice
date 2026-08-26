from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaApplication:
    applicationId: str = Field(default = "", validation_alias = "applicationId", serialization_alias = "applicationId")