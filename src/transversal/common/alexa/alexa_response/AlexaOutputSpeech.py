from pydantic.dataclasses import dataclass

@dataclass
class AlexaOutputSpeech:
    type: str = "SSML"  # "PlainText" o "SSML" para voz
    text: str = ""