from dataclasses import dataclass, field

from application.data_transfer_object.gemini.gemini_content import GeminiContent
from application.data_transfer_object.gemini.generation_config import GenerationConfig
from application.data_transfer_object.gemini.system_instruction import SystemInstruction

@dataclass
class GeminiPayload:
    system_instruction: SystemInstruction = field(default_factory = SystemInstruction)
    contents: list[GeminiContent] = field(default_factory = list)
    generation_config: GenerationConfig = field(default_factory = GenerationConfig)