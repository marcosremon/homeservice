from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, Header, HTTPException, Request, status
from application.data_transfer_object.alexa.alexa_request import AlexaRequest
from application.data_transfer_object.alexa.alexa_response import AlexaResponse
from application.interface.application.i_alexa_application import IAlexaApplication
from application.interface.security.i_alexa_request_verifier import IAlexaRequestVerifier
from infraestructure.persistence.dependencies.dependency_injection import get_alexa_application
from transversal.common.configuration.settings import Settings, get_settings
from transversal.common.utils import alexa_utils
from transversal.common.wrappers.json.base_response_json import BaseResponseJson
from transversal.common.wrappers.json.response_codes_json import ResponseCodesJson
from transversal.json_interchange.alexa.alexa_request_json import AlexaRequestJson
from transversal.json_interchange.alexa.alexa_response_json import AlexaResponseJson
from transversal.security.alexa.alexa_auth import get_alexa_request_verifier
from transversal.security.filter import debug_bypass

# Aqui no va Depends(get_api_key): quien llama es Amazon, que no conoce la
# X-Api-Key. La autorizacion es la firma del certificado, verificada dentro del
# endpoint porque el bypass de pruebas tiene que saltarsela igual que al skill id.
# Sin prefix: la ruta va entera en el decorador porque el endpoint cuelga
# de la raiz de la seccion ("/alexa"), sin subruta por debajo.
router = APIRouter()

@cbv(router)
class AlexaController:
    _alexa_application: IAlexaApplication = Depends(get_alexa_application)
    _alexa_request_verifier: IAlexaRequestVerifier = Depends(get_alexa_request_verifier)
    _settings: Settings = Depends(get_settings)

    #region SendAlexaOrder
    @router.post("/alexa", status_code = status.HTTP_200_OK)
    async def send_alexa_order(self, request: Request, x_debug_key: str = Header(default = "", alias = debug_bypass.HEADER_NAME)) -> AlexaResponseJson:
        alexa_response_json: AlexaResponseJson = AlexaResponseJson()
        try:
            # Bypass de pruebas (X-Debug-Key): omite firma de Amazon y validacion de skill.
            bypass: bool = debug_bypass.is_requested(x_debug_key)

            if not bypass and not await self._alexa_request_verifier.amazon_approve(request):
                alexa_response_json.base_response_json = BaseResponseJson(
                    response_code_json = ResponseCodesJson.UNAUTHORIZED,
                    message = "Llamada realizada por un dispositivo no autorizado",
                    is_success = False,
                )
            else:
                alexa_request_json: AlexaRequestJson | None = await alexa_utils.read_alexa_request_json(request)

                if alexa_request_json is None or alexa_request_json.alexa_request_data is None:
                    alexa_response_json.base_response_json = BaseResponseJson(
                        response_code_json = ResponseCodesJson.BAD_REQUEST,
                        message = "AlexaController -> send_alexa_order -> alexa_request_data nula o ausente",
                        is_success = False,
                    )
                elif not bypass and not alexa_utils.check_skill_origin(alexa_request_json.session, self._settings.alexa_skill_id):
                    alexa_response_json.base_response_json = BaseResponseJson(
                        response_code_json = ResponseCodesJson.UNAUTHORIZED,
                        message = "Peticion de una skill no autorizada",
                        is_success = False,
                    )
                else:
                    alexa_request: AlexaRequest = AlexaRequest(
                        version = alexa_request_json.version,
                        session = alexa_request_json.session,
                        alexa_request_data = alexa_request_json.alexa_request_data,
                        context = alexa_request_json.context,
                    )

                    alexa_response: AlexaResponse = await self._alexa_application.send_alexa_order(alexa_request)

                    alexa_response_json.version = alexa_response.version
                    alexa_response_json.session_attributes = alexa_response.session_attributes
                    alexa_response_json.alexa_response_content = alexa_response.alexa_response_content
                    alexa_response_json.base_response_json = BaseResponseJson(
                        response_code_json = ResponseCodesJson.OK,
                        message = "All success",
                        is_success = True,
                    )
        except Exception as ex:
            print(f"AlexaController -> send_alexa_order -> Error inesperado {ex}")
            alexa_response_json.base_response_json = BaseResponseJson(
                response_code_json = ResponseCodesJson.UNEXPECTED_ERROR,
                message = "Error inesperado al procesar la peticion",
                is_success = False,
            )

        if alexa_response_json.base_response_json.response_code_json == ResponseCodesJson.UNAUTHORIZED:
            raise HTTPException(status_code = status.HTTP_401_UNAUTHORIZED, detail = "Unauthorized")

        return alexa_response_json
    #endregion
