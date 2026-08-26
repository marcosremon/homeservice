from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.AlexaContext import AlexaContext
from transversal.common.alexa.alexa_request.AlexaRequestData import AlexaRequestData
from transversal.common.alexa.alexa_request.AlexaSession import AlexaSession

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaRequestJson:
    """Los alias son el equivalente a [JsonPropertyName]: Amazon manda "request",
    no "alexa_request_data".
    """
    version: str = "1.0"
    session: AlexaSession | None = None
    alexaRequestData: AlexaRequestData | None = Field(default = None, validation_alias = "request", serialization_alias = "request")
    context: AlexaContext | None = None