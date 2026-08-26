from dataclasses import dataclass, field

from application.data_transfer_object.gemini.GeminiContent import GeminiContent
from application.data_transfer_object.gemini.GenerationConfig import GenerationConfig
from application.data_transfer_object.gemini.SystemInstruction import SystemInstruction

@dataclass
class GeminiPayload:
    systemInstruction: SystemInstruction = field(default_factory = SystemInstruction)
    contents: list[GeminiContent] = field(default_factory = list)
    generationConfig: GenerationConfig = field(default_factory = GenerationConfig)