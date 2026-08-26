from dataclasses import dataclass, field

from application.data_transfer_object.gemini.gemini_part import GeminiPart

@dataclass
class GeminiContent:
    role: str = ""
    parts: list[GeminiPart] = field(default_factory = list)