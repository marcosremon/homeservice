import re
from datetime import timedelta

from fastapi import Request
from pydantic import TypeAdapter, ValidationError

from domain.model.enum.Alexa.intent_name import IntentName
from transversal.common.alexa.alexa_request.alexa_session import AlexaSession
from transversal.common.alexa.alexa_response.alexa_output_speech import AlexaOutputSpeech
from transversal.common.alexa.alexa_response.alexa_reprompt import AlexaReprompt
from transversal.common.alexa.alexa_response.alexa_response_content import AlexaResponseContent
from transversal.common.utils.general_utils import GeneralUtils
from transversal.json_interchange.alexa.alexa_request_json import AlexaRequestJson

_alexa_request_json_adapter: TypeAdapter[AlexaRequestJson] = TypeAdapter(AlexaRequestJson)

_DAYS_PATTERN: re.Pattern[str] = re.compile(r"(\d+)D")
_HOURS_PATTERN: re.Pattern[str] = re.compile(r"(\d+)H")
_MINUTES_PATTERN: re.Pattern[str] = re.compile(r"(\d+)M")
_SECONDS_PATTERN: re.Pattern[str] = re.compile(r"(\d+)S")

# region parse_alexa_duration
def parse_alexa_duration(iso_duration: str) -> timedelta:
    """Duracion ISO-8601 de Alexa (P1DT2H30M) a timedelta.

    Portado tal cual del C#, con su misma limitacion: la "M" de meses y la de
    minutos comparten patron, asi que "P2M" se lee como 2 minutos. Como Alexa solo
    manda duraciones cortas en los slots AMAZON.DURATION, no molesta.
    """
    total: timedelta = timedelta()

    if GeneralUtils.is_null_or_empty(iso_duration) or not iso_duration.startswith("P"):
        print(f"[parse_alexa_duration] Formato invalido: '{iso_duration}'")
        return total

    days_match: re.Match[str] | None = _DAYS_PATTERN.search(iso_duration)
    if days_match:
        total += timedelta(days = int(days_match.group(1)))

    hours_match: re.Match[str] | None = _HOURS_PATTERN.search(iso_duration)
    if hours_match:
        total += timedelta(hours = int(hours_match.group(1)))

    minutes_match: re.Match[str] | None = _MINUTES_PATTERN.search(iso_duration)
    if minutes_match:
        total += timedelta(minutes = int(minutes_match.group(1)))

    seconds_match: re.Match[str] | None = _SECONDS_PATTERN.search(iso_duration)
    if seconds_match:
        total += timedelta(seconds = int(seconds_match.group(1)))

    print(f"[parse_alexa_duration] '{iso_duration}' -> {total.total_seconds()}s")

    return total
# endregion

# region parse_alexa_order
def parse_alexa_order(alexa_order: IntentName) -> str:
    """El prefijo de intent que consumen los services. ConversationIntent no lo es."""
    if alexa_order in (IntentName.roomba_order_, IntentName.computer_status_order_, IntentName.light_order_):
        return alexa_order.name

    return ""
# endregion

# region build_alexa_response
def build_alexa_response(intent_name: str, message: str) -> AlexaResponseContent:
    """Respuesta de una orden: habla y cierra la sesion."""
    speech_text: str = "Comando recibido correctamente en el Home Lab." if GeneralUtils.is_null_or_empty(message) else message

    return AlexaResponseContent(
        output_speech = AlexaOutputSpeech(type = "SSML", text = speech_text),
        should_end_session = True,
    )
# endregion

# region build_conversation_response
def build_conversation_response(speech_text: str, keep_session_open: bool) -> AlexaResponseContent:
    """Respuesta de conversacion: si la sesion sigue abierta, lleva reprompt.

    Sin reprompt, Alexa cierra el microfono aunque should_end_session sea False.
    """
    speech: str = "Si?" if GeneralUtils.is_null_or_empty(speech_text) else speech_text

    alexa_response_content: AlexaResponseContent = AlexaResponseContent(
        output_speech = AlexaOutputSpeech(type = "PlainText", text = speech),
        should_end_session = not keep_session_open,
    )

    if keep_session_open:
        alexa_response_content.reprompt = AlexaReprompt(
            output_speech = AlexaOutputSpeech(
                type = "PlainText",
                text = "Sigues ahi? Puedes preguntarme otra cosa o decir para, para terminar.",
            )
        )

    return alexa_response_content
# endregion

# region check_skill_origin
def check_skill_origin(session: AlexaSession | None, skill_id: str) -> bool:
    """True si la peticion viene de la skill configurada.

    Amazon manda el id en session.application.applicationId. A diferencia del C#,
    esto falla cerrado: sin skill id configurado se rechaza en vez de aceptar.
    """
    if GeneralUtils.is_null_or_empty(skill_id) or session is None:
        return False

    return session.application.application_id == skill_id
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