from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.SystemState import SystemState

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaContext:
    # Amazon manda esta clave en mayuscula: "System", no "system".
    system: SystemState = Field(default_factory = SystemState, validation_alias = "System", serialization_alias = "System")