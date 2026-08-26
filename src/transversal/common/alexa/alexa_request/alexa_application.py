from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaApplication:
    application_id: str = Field(default = "", validation_alias = "applicationId", serialization_alias = "applicationId")