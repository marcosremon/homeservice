from dataclasses import dataclass, field
from typing import Any

@dataclass
class AlexaResponse:
    version: str = ""
    session_attributes: dict[str, str] | None = None
    alexa_response_content: dict[str, Any] = field(default_factory = dict)