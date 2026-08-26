from dataclasses import dataclass, field

from transversal.common.alexa.alexa_response.alexa_response_content import AlexaResponseContent

@dataclass
class AlexaResponse:
    version: str = ""
    session_attributes: dict[str, str] | None = None
    alexa_response_content: AlexaResponseContent = field(default_factory = AlexaResponseContent)