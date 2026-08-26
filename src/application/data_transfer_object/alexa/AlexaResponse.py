from dataclasses import dataclass, field

from transversal.common.alexa.alexa_response.AlexaResponseContent import AlexaResponseContent

@dataclass
class AlexaResponse:
    version: str = ""
    sessionAttributes: dict[str, str] | None = None
    alexaResponseContent: AlexaResponseContent = field(default_factory = AlexaResponseContent)