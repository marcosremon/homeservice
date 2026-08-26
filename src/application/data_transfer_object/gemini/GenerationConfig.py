from dataclasses import dataclass

from application.data_transfer_object.gemini.ThinkingConfig import ThinkingConfig

@dataclass
class GenerationConfig:
    maxOutputTokens: int = 0
    temperature: float = 0.0
    thinkingConfig: ThinkingConfig | None = None