from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_request.alexa_application import AlexaApplication
from transversal.common.alexa.alexa_request.alexa_user import AlexaUser

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaSession:
    new: bool = False
    session_id: str = Field(default = "", validation_alias = "sessionId", serialization_alias = "sessionId")
    application: AlexaApplication = Field(default_factory = AlexaApplication)
    user: AlexaUser = Field(default_factory = AlexaUser)
    attributes: dict[str, str] | None = None