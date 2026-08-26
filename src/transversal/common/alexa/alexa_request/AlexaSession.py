from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.AlexaApplication import AlexaApplication
from transversal.common.alexa.alexa_request.AlexaUser import AlexaUser

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaSession:
    new: bool = False
    sessionId: str = Field(default = "", validation_alias = "sessionId", serialization_alias = "sessionId")
    application: AlexaApplication = Field(default_factory = AlexaApplication)
    user: AlexaUser = Field(default_factory = AlexaUser)
    attributes: dict[str, str] | None = None