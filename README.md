# homeservice

API de domótica en ASP.NET Core (.NET 10): skill de Alexa, roomba, luces y termostatos por MQTT, sensores ESP32 de presencia, lluvia y temperatura, y control del ordenador por la LAN.

## Arrancar

1. Copia `.env.example` a `.env` y rellénalo. `DATABASE_URL` e `INTERNAL_API_KEY` son obligatorias.
2. `dotnet run --project HomeService.Service.WebApi`

Al arrancar, la app crea o actualiza el esquema de PostgreSQL. Por defecto escucha en `http://localhost:5279`, y en Development publica la especificación OpenAPI en `/openapi/v1.json`.

> Con el `.env` real, la app actúa sobre dispositivos de verdad. Para probar, mira [probar en local](docs/runbooks/probar-en-local.md).

## API

Todas las rutas cuelgan de `/api`. Las REST piden la cabecera `X-Api-Key`; `/api/alexa` se autentica con la firma de Amazon.

| Ruta | Para qué |
|---|---|
| `POST /api/alexa` | Skill de Alexa |
| `/api/change-computer-status/...` | Encender, apagar y consultar el ordenador |
| `/api/roomba/...` | Alta y estado de la roomba |
| `/api/sensor/presence-sensor/...` | Sensores de presencia |
| `/api/sensors/rain-sensor/...` | Sensores de lluvia |
| `/api/sensors/temperature-sensor/...` | Sensores de temperatura |

## Documentación

- [`CLAUDE.md`](CLAUDE.md): capas, convenciones, configuración y estado del proyecto.
- [`docs/decisions`](docs/decisions): por qué está hecho así lo que no es obvio.
- [`docs/runbooks`](docs/runbooks): probar en local, añadir una migración.
