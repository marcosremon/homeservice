from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.configuration.json_model_config import JSON_MODEL_CONFIG

from transversal.common.alexa.alexa_response.alexa_output_speech import AlexaOutputSpeech
from transversal.common.alexa.alexa_response.alexa_reprompt import AlexaReprompt

@dataclass(config = JSON_MODEL_CONFIG)
class AlexaResponseContent:
    output_speech: AlexaOutputSpeech = Field(default_factory = AlexaOutputSpeech, validation_alias = "outputSpeech", serialization_alias = "outputSpeech")
    reprompt: AlexaReprompt | None = None
    should_end_session: bool = Field(default = True, validation_alias = "shouldEndSession", serialization_alias = "shouldEndSession")