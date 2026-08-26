from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_response.AlexaResponseContent import AlexaResponseContent
from transversal.common.wrappers.json.BaseResponseJson import BaseResponseJson

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaResponseJson:
    version: str = "1.0"
    sessionAttributes: dict[str, str] | None = Field(default = None, validation_alias = "sessionAttributes", serialization_alias = "sessionAttributes")
    alexaResponseContent: AlexaResponseContent = Field(default_factory = AlexaResponseContent, validation_alias = "response", serialization_alias = "response")
    # Equivalente a [JsonIgnore]: es control interno del controlador, no se
    # serializa hacia Amazon. exclude = True es lo que lo saca del response_model.
    baseResponseJson: BaseResponseJson = Field(default_factory = BaseResponseJson, exclude = True)