from dataclasses import dataclass

@dataclass
class ThinkingConfig:
    thinkingBudget: int = 0  # 0 desactiva el "thinking" de gemini-2.5-flash