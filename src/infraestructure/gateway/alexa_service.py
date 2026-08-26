from pydantic import TypeAdapter, ValidationError

from application.data_transfer_object.alexa.alexa_request import AlexaRequest
from application.data_transfer_object.alexa.alexa_response import AlexaResponse
from application.data_transfer_object.gemini.gemini_turn import GeminiTurn
from application.data_transfer_object.gemini.gemini_turn_request import GeminiTurnRequest
from application.data_transfer_object.gemini.gemini_turn_response import GeminiTurnResponse
from application.interface.service.i_alexa_service import IAlexaService
from application.interface.service.i_computer_status_service import IComputerStatusService
from application.interface.service.i_gemini_service import IGeminiService
from application.interface.service.i_light_service import ILightService
from application.interface.service.i_roomba_service import IRoombaService
from domain.model.enum.Alexa.alexa_request_type import AlexaRequestType
from domain.model.enum.Alexa.intent_name import IntentName
from transversal.common.alexa.alexa_request.alexa_intent import AlexaIntent
from transversal.common.alexa.alexa_request.alexa_slot import AlexaSlot
from transversal.common.alexa.alexa_request.alexa_user import AlexaUser
from transversal.common.alexa.alexa_response.alexa_response_content import AlexaResponseContent
from transversal.common.configuration.settings import Settings, get_settings
from transversal.common.utils import alexa_utils
from transversal.common.utils.general_utils import GeneralUtils

_HISTORY_ATTRIBUTE: str = "history"
_QUERY_SLOT: str = "query"

_history_adapter: TypeAdapter[list[GeminiTurn]] = TypeAdapter(list[GeminiTurn])

class AlexaService(IAlexaService):

    def __init__(self, light_service: ILightService,
        roomba_service: IRoombaService,
        gemini_service: IGeminiService,
        computer_status_service: IComputerStatusService,
        settings: Settings,
    ):
        self._light_service = light_service
        self._roomba_service = roomba_service
        self._gemini_service = gemini_service
        self._computer_status_service = computer_status_service
        self._settings = settings

    # region send_alexa_order
    async def send_alexa_order(self, alexa_request: AlexaRequest) -> AlexaResponse:
        alexa_response: AlexaResponse = AlexaResponse()
        try:
            intent: AlexaIntent | None = alexa_request.alexa_request_data.intent
            speech_text: str = ""
            keep_session_open: bool = False

            is_launch_request: bool = GeneralUtils.parse_enum_exact(AlexaRequestType, alexa_request.alexa_request_data.type) == AlexaRequestType.LaunchRequest
            if is_launch_request:
                speech_text = "Hola, ya puedes hablar conmigo. ¿Qué quieres saber?"
                keep_session_open = True
            elif (intent_name := GeneralUtils.parse_enum_exact(IntentName, intent.name if intent is not None else None)) is not None:
                if intent_name == IntentName.ConversationIntent:
                    query_slot: AlexaSlot | None = intent.slots.get("query") if intent is not None and intent.slots is not None else None
                    user_text: str = query_slot.value if query_slot is not None and query_slot.value is not None else ""
                    if GeneralUtils.is_null_or_white_space(user_text):
                        speech_text = "No te he entendido, ¿puedes repetir?"
                        keep_session_open = True
                    else:
                        history: list[GeminiTurn] = self._read_history(alexa_request)
                        gemini_turn_request: GeminiTurnRequest = GeminiTurnRequest(
                            user_text = user_text,
                            history = history,
                        )

                        gemini_turn_response: GeminiTurnResponse = await self._gemini_service.converse(gemini_turn_request)
                        self._write_history(alexa_response, gemini_turn_response.updated_history)

                        speech_text = gemini_turn_response.replay
                        keep_session_open = True
                else:
                    message: str = ""
                    intent_name_string = intent_name.name

                    if intent_name == IntentName.roomba_order_: message = await self._roomba_service._execute_roomba_order(intent_name_string, alexa_request)
                    if intent_name == IntentName.light_order_: message = await self._light_service.execute_light_order(intent_name_string)
                    if intent_name == IntentName.computer_status_order_: message = await self._computer_status_service.execute_computer_status_order(intent_name_string)

                    alexa_response.alexa_response_content = alexa_utils.build_alexa_response(intent_name_string, message)
            else:
                print(f"AlexaService -> intent desconocido: {intent.name if intent is not None else None}")
                speech_text = "No te he entendido, ¿puedes repetir?"
                keep_session_open = True

            alexa_response.version = get_settings().alexa_version
            if keep_session_open: alexa_response.alexa_response_content = alexa_utils.build_conversation_response(speech_text, keep_session_open)
        except Exception as ex:
            print(f"Unexpected error on AlexaService -> send_alexa_order -> {ex}")

        return alexa_response
    # endregion

    # region _read_history
    @staticmethod
    def _read_history(alexa_request: AlexaRequest) -> list[GeminiTurn]:
        """El historial de la conversacion viaja en los sessionAttributes.

        Alexa devuelve en cada turno lo que le mandamos en el anterior, asi que
        es el unico "estado" que tiene la skill.
        """
        try:
            attributes: dict[str, str] | None = alexa_request.session.attributes if alexa_request.session is not None else None
            if attributes is not None:
                history_json: str = attributes.get(_HISTORY_ATTRIBUTE, "")
                if not GeneralUtils.is_null_or_empty(history_json):
                    return _history_adapter.validate_json(history_json)
        except (ValueError, ValidationError) as ex:
            print(f"[History read] {ex}")

        return []
    # endregion

    # region _write_history
    @staticmethod
    def _write_history(alexa_response: AlexaResponse, history: list[GeminiTurn]) -> None:
        try:
            alexa_response.session_attributes = {
                _HISTORY_ATTRIBUTE: _history_adapter.dump_json(history).decode("utf-8"),
            }
        except (ValueError, ValidationError) as ex:
            print(f"[History write] {ex}")
    # endregion