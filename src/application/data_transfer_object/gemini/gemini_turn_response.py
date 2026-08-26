from dataclasses import dataclass, field

from application.data_transfer_object.gemini.gemini_turn import GeminiTurn

@dataclass
class GeminiTurnResponse:
    replay: str = ""
    updated_history: list[GeminiTurn] = field(default_factory = list)