from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.alexa_context import AlexaContext
from transversal.common.alexa.alexa_request.alexa_request_data import AlexaRequestData
from transversal.common.alexa.alexa_request.alexa_session import AlexaSession

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaRequestJson:
    """Los alias son el equivalente a [JsonPropertyName]: Amazon manda "request",
    no "alexa_request_data".
    """
    version: str = "1.0"
    session: AlexaSession | None = None
    alexa_request_data: AlexaRequestData | None = Field(default = None, validation_alias = "request", serialization_alias = "request")
    context: AlexaContext | None = None