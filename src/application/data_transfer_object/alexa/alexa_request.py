from dataclasses import dataclass, field

from transversal.common.alexa.alexa_request.alexa_context import AlexaContext
from transversal.common.alexa.alexa_request.alexa_request_data import AlexaRequestData
from transversal.common.alexa.alexa_request.alexa_session import AlexaSession

@dataclass
class AlexaRequest:
    version: str = "1.0"
    session: AlexaSession | None = None
    alexa_request_data: AlexaRequestData = field(default_factory = AlexaRequestData)
    context: AlexaContext | None = None