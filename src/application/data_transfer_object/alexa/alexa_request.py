from dataclasses import dataclass, field
from typing import Any

@dataclass
class AlexaRequest:
    version: str = "1.0"
    session: dict[str, Any] | None = None
    alexa_request_data: dict[str, Any] = field(default_factory = dict)
    context: dict[str, Any] | None = None