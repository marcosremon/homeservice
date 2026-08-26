from typing import Any

from fastapi import Request
from pydantic import TypeAdapter, ValidationError

from transversal.common.utils.general_utils import GenericUtils
from transversal.json_interchange.alexa.alexa_request_json import AlexaRequestJson

_alexa_request_json_adapter: TypeAdapter[AlexaRequestJson] = TypeAdapter(AlexaRequestJson)


# region check_skill_origin
def check_skill_origin(session: dict[str, Any] | None, skill_id: str) -> bool:
    """True si la peticion viene de la skill configurada.

    Amazon manda el id en session.application.applicationId. Falla cerrado: sin
    skill id configurado o sin sesion, se rechaza.
    """
    if GenericUtils.is_null_or_empty(skill_id) or not session:
        return False

    application: Any = session.get("application")
    if not isinstance(application, dict):
        return False

    return application.get("applicationId") == skill_id
# endregion


# region read_alexa_request_json
async def read_alexa_request_json(request: Request) -> AlexaRequestJson | None:
    """
    El cuerpo no se declara como parametro del endpoint porque el verificador de
    firma necesita leerlo en crudo antes; aqui ya viene cacheado por Starlette.
    El TypeAdapter es lo que hace respetar los alias ("request", "session").
    """
    try:
        return _alexa_request_json_adapter.validate_python(await request.json())
    except (ValueError, ValidationError):
        return None
# endregion
