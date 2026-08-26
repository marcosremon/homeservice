from dataclasses import dataclass, field

from transversal.common.alexa.alexa_request.AlexaContext import AlexaContext
from transversal.common.alexa.alexa_request.AlexaRequestData import AlexaRequestData
from transversal.common.alexa.alexa_request.AlexaSession import AlexaSession

@dataclass
class AlexaRequest:
    version: str = "1.0"
    session: AlexaSession | None = None
    alexaRequestData: AlexaRequestData = field(default_factory = AlexaRequestData)
    context: AlexaContext | None = None