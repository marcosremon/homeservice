from dataclasses import dataclass, field

from application.data_transfer_object.gemini.gemini_turn import GeminiTurn

@dataclass
class GeminiTurnRequest:
    user_text: str = ""
    history: list[GeminiTurn] = field(default_factory = list)