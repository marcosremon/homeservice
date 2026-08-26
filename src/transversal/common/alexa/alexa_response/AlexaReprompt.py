from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_response.AlexaOutputSpeech import AlexaOutputSpeech

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaReprompt:
    outputSpeech: AlexaOutputSpeech = Field(default_factory = AlexaOutputSpeech, validation_alias = "outputSpeech", serialization_alias = "outputSpeech")