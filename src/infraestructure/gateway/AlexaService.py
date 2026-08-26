from pydantic import TypeAdapter, ValidationError

from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.data_transfer_object.alexa.AlexaResponse import AlexaResponse
from application.data_transfer_object.gemini.GeminiTurn import GeminiTurn
from application.data_transfer_object.gemini.GeminiTurnRequest import GeminiTurnRequest
from application.data_transfer_object.gemini.GeminiTurnResponse import GeminiTurnResponse
from application.interface.service.IAlexaService import IAlexaService
from application.interface.service.IComputerStatusService import IComputerStatusService
from application.interface.service.IGeminiService import IGeminiService
from application.interface.service.ILightService import ILightService
from application.interface.service.IRoombaService import IRoombaService
from domain.model.enum.Alexa.AlexaRequestType import AlexaRequestType
from domain.model.enum.Alexa.IntentName import IntentName
from transversal.common.alexa.alexa_request.AlexaIntent import AlexaIntent
from transversal.common.alexa.alexa_request.AlexaSlot import AlexaSlot
from transversal.common.configuration.Settings import Settings, GetSettings
from transversal.common.utils import AlexaUtils
from transversal.common.utils.GeneralUtils import GeneralUtils

_HISTORY_ATTRIBUTE: str = "history"
_QUERY_SLOT: str = "query"

_historyAdapter: TypeAdapter[list[GeminiTurn]] = TypeAdapter(list[GeminiTurn])

class AlexaService(IAlexaService):

    def __init__(self, lightService: ILightService,
        roombaService: IRoombaService,
        geminiService: IGeminiService,
        computerStatusService: IComputerStatusService,
        settings: Settings,
    ):
        self._lightService = lightService
        self._roombaService = roombaService
        self._geminiService = geminiService
        self._computerStatusService = computerStatusService
        self._settings = settings

    # region send_alexa_order
    async def SendAlexaOrder(self, alexaRequest: AlexaRequest) -> AlexaResponse:
        alexaResponse: AlexaResponse = AlexaResponse()
        try:
            intent: AlexaIntent | None = alexaRequest.alexaRequestData.intent
            speechText: str = ""
            keepSessionOpen: bool = False

            isLaunchRequest: bool = GeneralUtils.ParseEnumExact(AlexaRequestType, alexaRequest.alexaRequestData.type) == AlexaRequestType.LaunchRequest
            if isLaunchRequest:
                speechText = "Hola, ya puedes hablar conmigo. ¿Qué quieres saber?"
                keepSessionOpen = True
            elif (intentName := GeneralUtils.ParseEnumExact(IntentName, intent.name if intent is not None else None)) is not None:
                if intentName == IntentName.ConversationIntent:
                    querySlot: AlexaSlot | None = intent.slots.get("query") if intent is not None and intent.slots is not None else None
                    userText: str = querySlot.value if querySlot is not None and querySlot.value is not None else ""
                    if GeneralUtils.IsNullOrWhiteSpace(userText):
                        speechText = "No te he entendido, ¿puedes repetir?"
                        keepSessionOpen = True
                    else:
                        history: list[GeminiTurn] = self._readHistory(alexaRequest)
                        geminiTurnRequest: GeminiTurnRequest = GeminiTurnRequest(
                            userText = userText,
                            history = history,
                        )

                        geminiTurnResponse: GeminiTurnResponse = await self._geminiService.converse(geminiTurnRequest)
                        self._writeHistory(alexaResponse, geminiTurnResponse.updatedHistory)

                        speechText = geminiTurnResponse.replay
                        keepSessionOpen = True
                else:
                    message: str = ""
                    intentNameString = intentName.name

                    if intentName == IntentName.roomba_order_: message = await self._roombaService.ExecuteRoombaOrder(intentNameString, alexaRequest)
                    if intentName == IntentName.light_order_: message = await self._lightService.ExecuteLightOrder(intentNameString)
                    if intentName == IntentName.computer_status_order_: message = await self._computerStatusService.ExecuteComputerStatusOrder(intentNameString)

                    alexaResponse.alexaResponseContent = AlexaUtils.BuildAlexaResponse(intentNameString, message)
            else:
                print(f"AlexaService -> intent desconocido: {intent.name if intent is not None else None}")
                speechText = "No te he entendido, ¿puedes repetir?"
                keepSessionOpen = True

            alexaResponse.version = GetSettings().alexaVersion
            if keepSessionOpen: alexaResponse.alexaResponseContent = AlexaUtils.BuildConversationResponse(speechText, keepSessionOpen)
        except Exception as ex:
            print(f"Unexpected error on AlexaService -> send_alexa_order -> {ex}")

        return alexaResponse
    # endregion

    # region _read_history
    @staticmethod
    def _readHistory(alexaRequest: AlexaRequest) -> list[GeminiTurn]:
        """El historial de la conversacion viaja en los sessionAttributes.

        Alexa devuelve en cada turno lo que le mandamos en el anterior, asi que
        es el unico "estado" que tiene la skill.
        """
        try:
            attributes: dict[str, str] | None = alexaRequest.session.attributes if alexaRequest.session is not None else None
            if attributes is not None:
                historyJson: str = attributes.get(_HISTORY_ATTRIBUTE, "")
                if not GeneralUtils.IsNullOrEmpty(historyJson):
                    return _historyAdapter.validate_json(historyJson)
        except (ValueError, ValidationError) as ex:
            print(f"[History read] {ex}")

        return []
    # endregion

    # region _write_history
    @staticmethod
    def _writeHistory(alexaResponse: AlexaResponse, history: list[GeminiTurn]) -> None:
        try:
            alexaResponse.sessionAttributes = {
                _HISTORY_ATTRIBUTE: _historyAdapter.dump_json(history).decode("utf-8"),
            }
        except (ValueError, ValidationError) as ex:
            print(f"[History write] {ex}")
    # endregion