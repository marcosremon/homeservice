from dataclasses import dataclass

@dataclass
class GeminiTurn:
    role: str = ""  # "user" o "model"
    text: str = ""