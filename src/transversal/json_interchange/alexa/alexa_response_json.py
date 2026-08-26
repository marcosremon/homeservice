from typing import Any

from pydantic import Field
from pydantic.dataclasses import dataclass

from transversal.common.wrappers.json.base_response_json import BaseResponseJson

@dataclass
class AlexaResponseJson:
    version: str = "1.0"
    session_attributes: dict[str, str] | None = Field(default = None, alias = "sessionAttributes")
    alexa_response_content: dict[str, Any] = Field(default_factory = dict, alias = "response")
    base_response_json: BaseResponseJson = Field(default_factory = BaseResponseJson, exclude = True)