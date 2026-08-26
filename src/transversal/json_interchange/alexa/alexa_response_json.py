from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_response.alexa_response_content import AlexaResponseContent
from transversal.common.wrappers.json.base_response_json import BaseResponseJson

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaResponseJson:
    version: str = "1.0"
    session_attributes: dict[str, str] | None = Field(default = None, validation_alias = "sessionAttributes", serialization_alias = "sessionAttributes")
    alexa_response_content: AlexaResponseContent = Field(default_factory = AlexaResponseContent, validation_alias = "response", serialization_alias = "response")
    # Equivalente a [JsonIgnore]: es control interno del controlador, no se
    # serializa hacia Amazon. exclude = True es lo que lo saca del response_model.
    base_response_json: BaseResponseJson = Field(default_factory = BaseResponseJson, exclude = True)