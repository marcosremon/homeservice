from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.alexa.alexa_request.AlexaApplication import AlexaApplication
from transversal.common.alexa.alexa_request.AlexaUser import AlexaUser

@dataclass
class SystemState:
    application: AlexaApplication = Field(default_factory = AlexaApplication)
    user: AlexaUser = Field(default_factory = AlexaUser)