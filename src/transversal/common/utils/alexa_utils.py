"""Equivalente a AlexaUtils de C#: modulo con funciones sueltas, no clase."""

from typing import Any

from transversal.common.utils.general_utils import GenericUtils


# region check_skill_origin
def check_skill_origin(session: dict[str, Any] | None, skill_id: str) -> bool:
    """True si la peticion viene de la skill configurada.

    Amazon manda el id en session.application.applicationId. Falla cerrado: sin
    skill id configurado o sin sesion, se rechaza.
    """
    if GenericUtils.is_null_or_empty(skill_id) or not session:
        return False

    application = session.get("application")
    if not isinstance(application, dict):
        return False

    return application.get("applicationId") == skill_id
# endregion
