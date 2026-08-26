from dataclasses import dataclass

from application.data_transfer_object.gemini.thinking_config import ThinkingConfig

@dataclass
class GenerationConfig:
    max_output_tokens: int = 0
    temperature: float = 0.0
    thinking_config: ThinkingConfig | None = None