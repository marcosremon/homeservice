from application.data_transfer_object.alexa.alexa_request import AlexaRequest
from application.data_transfer_object.alexa.alexa_response import AlexaResponse
from application.interface.application.i_alexa_application import IAlexaApplication

class AlexaApplication(IAlexaApplication):

    async def send_alexa_order(self, alexa_request: AlexaRequest) -> AlexaResponse:
        # TODO (Gateway): en C# esta clase solo delega en IAlexaService, que es quien enruta
        # por IntentName (roomba / luces / ordenador) y habla con Gemini para el
        # ConversationIntent. Falta portar ese servicio y inyectarlo aqui. De momento
        # se devuelve una respuesta valida para Alexa para no romper el contrato del skill.
        return AlexaResponse(
            version = alexa_request.version,
            alexa_response_content = {
                "outputSpeech": {"type": "PlainText", "text": "Funcionalidad todavia no disponible."},
                "shouldEndSession": True,
            },
        )