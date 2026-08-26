from dataclasses import dataclass, field
from typing import Any

@dataclass
class AlexaRequest:
    """Los modelos anidados de Alexa (AlexaSession, AlexaRequestData, AlexaContext)
    todavia no estan portados desde Transversal.Common.Alexa, asi que de momento
    viajan como dict tal cual llegan de Amazon.
    """
    version: str = "1.0"
    session: dict[str, Any] | None = None
    alexa_request_data: dict[str, Any] = field(default_factory = dict)
    context: dict[str, Any] | None = None
