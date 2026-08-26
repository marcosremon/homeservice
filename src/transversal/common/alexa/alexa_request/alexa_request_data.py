from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.alexa_intent import AlexaIntent

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaRequestData:
    type: str = ""
    request_id: str = Field(default = "", validation_alias = "requestId", serialization_alias = "requestId")
    timestamp: str = ""
    intent: AlexaIntent | None = None