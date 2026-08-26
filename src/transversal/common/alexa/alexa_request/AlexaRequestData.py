from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.AlexaIntent import AlexaIntent

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaRequestData:
    type: str = ""
    requestId: str = Field(default = "", validation_alias = "requestId", serialization_alias = "requestId")
    timestamp: str = ""
    intent: AlexaIntent | None = None