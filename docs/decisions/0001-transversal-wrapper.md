# 0001 — `Transversal.Wrapper` para romper un ciclo de referencias

**Estado**: aceptada

## Contexto

En el proyecto Python, `transversal/common` mezcla los tipos base (`BaseResponse`, los payloads de Alexa) con utilidades que importan hacia arriba: `RoombaUtils` y `RoombaState` usan `PatchRoombaStateRequest` y `RoombaActivatedEvent`, y `AlexaUtils` usa `AlexaRequestJson`.

Python lo permite. En C#, `Application.DataTransferObject` y `Transversal.JsonInterchange` necesitan `BaseResponse`, y `Transversal.Common` los necesita a ellos: referencia circular.

## Decisión

Las utilidades se quedan en `Transversal.Common`, con la misma ubicación que en Python. Los tipos que necesitan las capas de abajo se sacan a un proyecto nuevo sin dependencias, `Transversal.Wrapper`:

- `Base/`: `BaseResponse`, `ResponseCodes`
- `Json/`: `BaseResponseJson`, `ResponseCodesJson`
- `Alexa/AlexaRequest/` y `Alexa/AlexaResponse/`: los payloads de Amazon

`Alexa/` también tuvo que moverse, no solo los wrappers: `AlexaRequestJson` usa `AlexaSession`, y `AlexaUtils` usa `AlexaRequestJson`.

## Consecuencias

- `Transversal.Common` no es transversal en sentido estricto: está por encima de DTO, Event y JsonInterchange.
- Cualquier tipo que necesiten DTO o JsonInterchange va en `Transversal.Wrapper` o en `Domain.Model`, nunca en `Transversal.Common`.

**Alternativa descartada**: subir `RoombaUtils`, `RoombaState` y `AlexaUtils` a `Infraestructure.Gateway` y dejar `Transversal.Common` como capa base. Era más limpio, pero rompía el espejo con el proyecto Python.
