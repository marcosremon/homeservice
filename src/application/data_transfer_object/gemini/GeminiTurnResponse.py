from dataclasses import dataclass, field

from application.data_transfer_object.gemini.GeminiTurn import GeminiTurn

@dataclass
class GeminiTurnResponse:
    replay: str = ""
    updatedHistory: list[GeminiTurn] = field(default_factory = list)