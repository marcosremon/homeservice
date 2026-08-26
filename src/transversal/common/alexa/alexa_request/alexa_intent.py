from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.alexa_slot import AlexaSlot

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaIntent:
    name: str = ""
    confirmation_status: str = Field(default = "NONE", validation_alias = "confirmationStatus", serialization_alias = "confirmationStatus")
    slots: dict[str, AlexaSlot] | None = None