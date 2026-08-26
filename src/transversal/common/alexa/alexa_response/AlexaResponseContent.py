from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_response.AlexaOutputSpeech import AlexaOutputSpeech
from transversal.common.alexa.alexa_response.AlexaReprompt import AlexaReprompt

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaResponseContent:
    outputSpeech: AlexaOutputSpeech = Field(default_factory = AlexaOutputSpeech, validation_alias = "outputSpeech", serialization_alias = "outputSpeech")
    reprompt: AlexaReprompt | None = None
    shouldEndSession: bool = Field(default = True, validation_alias = "shouldEndSession", serialization_alias = "shouldEndSession")