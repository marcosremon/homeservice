import re
from datetime import timedelta

from fastapi import Request
from pydantic import TypeAdapter, ValidationError

from domain.model.enum.Alexa.IntentName import IntentName
from transversal.common.alexa.alexa_request.AlexaSession import AlexaSession
from transversal.common.alexa.alexa_response.AlexaOutputSpeech import AlexaOutputSpeech
from transversal.common.alexa.alexa_response.AlexaReprompt import AlexaReprompt
from transversal.common.alexa.alexa_response.AlexaResponseContent import AlexaResponseContent
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.json_interchange.alexa.AlexaRequestJson import AlexaRequestJson

_alexaRequestJsonAdapter: TypeAdapter[AlexaRequestJson] = TypeAdapter(AlexaRequestJson)

_DAYS_PATTERN: re.Pattern[str] = re.compile(r"(\d+)D")
_HOURS_PATTERN: re.Pattern[str] = re.compile(r"(\d+)H")
_MINUTES_PATTERN: re.Pattern[str] = re.compile(r"(\d+)M")
_SECONDS_PATTERN: re.Pattern[str] = re.compile(r"(\d+)S")

# region parse_alexa_duration
def ParseAlexaDuration(isoDuration: str) -> timedelta:
    """Duracion ISO-8601 de Alexa (P1DT2H30M) a timedelta.

    Portado tal cual del C#, con su misma limitacion: la "M" de meses y la de
    minutos comparten patron, asi que "P2M" se lee como 2 minutos. Como Alexa solo
    manda duraciones cortas en los slots AMAZON.DURATION, no molesta.
    """
    total: timedelta = timedelta()

    if GeneralUtils.IsNullOrEmpty(isoDuration) or not isoDuration.startswith("P"):
        print(f"[parse_alexa_duration] Formato invalido: '{isoDuration}'")
        return total

    daysMatch: re.Match[str] | None = _DAYS_PATTERN.search(isoDuration)
    if daysMatch:
        total += timedelta(days = int(daysMatch.group(1)))

    hoursMatch: re.Match[str] | None = _HOURS_PATTERN.search(isoDuration)
    if hoursMatch:
        total += timedelta(hours = int(hoursMatch.group(1)))

    minutesMatch: re.Match[str] | None = _MINUTES_PATTERN.search(isoDuration)
    if minutesMatch:
        total += timedelta(minutes = int(minutesMatch.group(1)))

    secondsMatch: re.Match[str] | None = _SECONDS_PATTERN.search(isoDuration)
    if secondsMatch:
        total += timedelta(seconds = int(secondsMatch.group(1)))

    print(f"[parse_alexa_duration] '{isoDuration}' -> {total.total_seconds()}s")

    return total
# endregion

# region parse_alexa_order
def ParseAlexaOrder(alexaOrder: IntentName) -> str:
    """El prefijo de intent que consumen los services. ConversationIntent no lo es."""
    if alexaOrder in (IntentName.roomba_order_, IntentName.computer_status_order_, IntentName.light_order_):
        return alexaOrder.name

    return ""
# endregion

# region build_alexa_response
def BuildAlexaResponse(intentName: str, message: str) -> AlexaResponseContent:
    """Respuesta de una orden: habla y cierra la sesion."""
    speechText: str = "Comando recibido correctamente en el Home Lab." if GeneralUtils.IsNullOrEmpty(message) else message

    return AlexaResponseContent(
        outputSpeech = AlexaOutputSpeech(type = "SSML", text = speechText),
        shouldEndSession = True,
    )
# endregion

# region build_conversation_response
def BuildConversationResponse(speechText: str, keepSessionOpen: bool) -> AlexaResponseContent:
    """Respuesta de conversacion: si la sesion sigue abierta, lleva reprompt.

    Sin reprompt, Alexa cierra el microfono aunque should_end_session sea False.
    """
    speech: str = "Si?" if GeneralUtils.IsNullOrEmpty(speechText) else speechText

    alexaResponseContent: AlexaResponseContent = AlexaResponseContent(
        outputSpeech = AlexaOutputSpeech(type = "PlainText", text = speech),
        shouldEndSession = not keepSessionOpen,
    )

    if keepSessionOpen:
        alexaResponseContent.reprompt = AlexaReprompt(
            outputSpeech = AlexaOutputSpeech(
                type = "PlainText",
                text = "Sigues ahi? Puedes preguntarme otra cosa o decir para, para terminar.",
            )
        )

    return alexaResponseContent
# endregion

# region check_skill_origin
def CheckSkillOrigin(session: AlexaSession | None, skillId: str) -> bool:
    """True si la peticion viene de la skill configurada.

    Amazon manda el id en session.application.applicationId. A diferencia del C#,
    esto falla cerrado: sin skill id configurado se rechaza en vez de aceptar.
    """
    if GeneralUtils.IsNullOrEmpty(skillId) or session is None:
        return False

    return session.application.applicationId == skillId
# endregion

# region read_alexa_request_json
async def ReadAlexaRequestJson(request: Request) -> AlexaRequestJson | None:
    """
    El cuerpo no se declara como parametro del endpoint porque el verificador de
    firma necesita leerlo en crudo antes; aqui ya viene cacheado por Starlette.
    El TypeAdapter es lo que hace respetar los alias ("request", "session").
    """
    try:
        return _alexaRequestJsonAdapter.validate_python(await request.json())
    except (ValueError, ValidationError):
        return None
# endregion