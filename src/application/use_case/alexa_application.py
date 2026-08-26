from application.data_transfer_object.alexa.alexa_request import AlexaRequest
from application.data_transfer_object.alexa.alexa_response import AlexaResponse
from application.interface.application.i_alexa_application import IAlexaApplication

class AlexaApplication(IAlexaApplication):

    async def send_alexa_order(self, alexa_request: AlexaRequest) -> AlexaResponse:
        # TODO: portar AlexaApplication de C#: enrutado por IntentName, dialogo con
        # Gemini y llamada a las aplicaciones de luces / roomba / ordenador. De momento
        # devuelve una respuesta valida para Alexa para no romper el contrato del skill.
        return AlexaResponse(
            version = alexa_request.version,
            alexa_response_content = {
                "outputSpeech": {"type": "PlainText", "text": "Funcionalidad todavia no disponible."},
                "shouldEndSession": True,
            },
        )
