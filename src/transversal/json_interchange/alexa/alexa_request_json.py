from typing import Any

from pydantic import Field
from pydantic.dataclasses import dataclass

@dataclass
class AlexaRequestJson:
    """Equivalente a AlexaRequestJson de C#.

    El alias es el equivalente a [JsonPropertyName]: Amazon manda "request" y
    "session", no "alexa_request_data". populate_by_name deja construirlo tambien
    con el nombre Python en los tests.
    """
    version: str = "1.0"
    session: dict[str, Any] | None = None
    alexa_request_data: dict[str, Any] | None = Field(default = None, alias = "request")
    context: dict[str, Any] | None = None
