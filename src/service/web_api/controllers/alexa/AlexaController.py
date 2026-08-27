from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, Header, HTTPException, Request, status
from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.data_transfer_object.alexa.AlexaResponse import AlexaResponse
from application.interface.application.IAlexaApplication import IAlexaApplication
from application.interface.security.IAlexaRequestVerifier import IAlexaRequestVerifier
from infraestructure.persistence.dependencies.DependencyInjection import GetAlexaApplication
from transversal.common.configuration.Settings import Settings, GetSettings
from transversal.common.utils.AlexaUtils import AlexaUtils
from transversal.common.wrappers.json.BaseResponseJson import BaseResponseJson
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson
from transversal.json_interchange.alexa.AlexaRequestJson import AlexaRequestJson
from transversal.json_interchange.alexa.AlexaResponseJson import AlexaResponseJson
from transversal.security.alexa.AlexaAuth import AlexaAuth
from transversal.security.filter.DebugBypass import DebugBypass

# Aqui no va Depends(get_api_key): quien llama es Amazon, que no conoce la
# X-Api-Key. La autorizacion es la firma del certificado, verificada dentro del
# endpoint porque el bypass de pruebas tiene que saltarsela igual que al skill id.
# Sin prefix: la ruta va entera en el decorador porque el endpoint cuelga
# de la raiz de la seccion ("/alexa"), sin subruta por debajo.
router = APIRouter()

@cbv(router)
class AlexaController:
    _alexaApplication: IAlexaApplication = Depends(GetAlexaApplication)
    _alexaRequestVerifier: IAlexaRequestVerifier = Depends(AlexaAuth.GetAlexaRequestVerifier)
    _settings: Settings = Depends(GetSettings)

    #region SendAlexaOrder
    @router.post("/alexa", status_code = status.HTTP_200_OK)
    async def SendAlexaOrder(self, request: Request, xDebugKey: str = Header(default = "", alias = DebugBypass.HEADER_NAME)) -> AlexaResponseJson:
        alexaResponseJson: AlexaResponseJson = AlexaResponseJson()
        try:
            # Bypass de pruebas (X-Debug-Key): omite firma de Amazon y validacion de skill.
            bypass: bool = DebugBypass.IsRequested(xDebugKey)

            if not bypass and not await self._alexaRequestVerifier.AmazonApprove(request):
                alexaResponseJson.baseResponseJson = BaseResponseJson(
                    responseCodeJson = ResponseCodesJson.UNAUTHORIZED,
                    message = "Llamada realizada por un dispositivo no autorizado",
                    isSuccess = False,
                )
            else:
                alexaRequestJson: AlexaRequestJson | None = await AlexaUtils.ReadAlexaRequestJson(request)

                if alexaRequestJson is None or alexaRequestJson.alexaRequestData is None:
                    alexaResponseJson.baseResponseJson = BaseResponseJson(
                        responseCodeJson = ResponseCodesJson.BAD_REQUEST,
                        message = "AlexaController -> send_alexa_order -> alexa_request_data nula o ausente",
                        isSuccess = False,
                    )
                elif not bypass and not AlexaUtils.CheckSkillOrigin(alexaRequestJson.session, self._settings.alexaSkillId):
                    alexaResponseJson.baseResponseJson = BaseResponseJson(
                        responseCodeJson = ResponseCodesJson.UNAUTHORIZED,
                        message = "Peticion de una skill no autorizada",
                        isSuccess = False,
                    )
                else:
                    alexaRequest: AlexaRequest = AlexaRequest(
                        version = alexaRequestJson.version,
                        session = alexaRequestJson.session,
                        alexaRequestData = alexaRequestJson.alexaRequestData,
                        context = alexaRequestJson.context,
                    )

                    alexaResponse: AlexaResponse = await self._alexaApplication.SendAlexaOrder(alexaRequest)

                    alexaResponseJson.version = alexaResponse.version
                    alexaResponseJson.sessionAttributes = alexaResponse.sessionAttributes
                    alexaResponseJson.alexaResponseContent = alexaResponse.alexaResponseContent
                    alexaResponseJson.baseResponseJson = BaseResponseJson(
                        responseCodeJson = ResponseCodesJson.OK,
                        message = "All success",
                        isSuccess = True,
                    )
        except Exception as ex:
            print(f"AlexaController -> send_alexa_order -> Error inesperado {ex}")
            alexaResponseJson.baseResponseJson = BaseResponseJson(
                responseCodeJson = ResponseCodesJson.UNEXPECTED_ERROR,
                message = "Error inesperado al procesar la peticion",
                isSuccess = False,
            )

        if alexaResponseJson.baseResponseJson.responseCodeJson == ResponseCodesJson.UNAUTHORIZED:
            raise HTTPException(status_code = status.HTTP_401_UNAUTHORIZED, detail = "Unauthorized")

        return alexaResponseJson
    #endregion