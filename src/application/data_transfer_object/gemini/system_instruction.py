from dataclasses import dataclass, field

from application.data_transfer_object.gemini.gemini_part import GeminiPart

@dataclass
class SystemInstruction:
    parts: list[GeminiPart] = field(default_factory = list)