from typing import Any

from pydantic import Field
from pydantic.dataclasses import dataclass

@dataclass
class AlexaRequestJson:
    version: str = "1.0"
    session: dict[str, Any] | None = None
    alexa_request_data: dict[str, Any] | None = Field(default = None, alias = "request")
    context: dict[str, Any] | None = None