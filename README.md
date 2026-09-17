# HomeService

API de domótica para el homelab. Recibe órdenes de voz desde una skill de **Alexa** y controla la Roomba, las luces, el termostato y el ordenador de casa. También recoge datos de sensores de presencia, lluvia y temperatura. Las preguntas que no son órdenes se responden con **Gemini**.

Hecho con **FastAPI** + **SQLAlchemy async** sobre **PostgreSQL**, siguiendo la misma arquitectura por capas que los servicios en .NET.

## Funcionalidades

- **Alexa**: endpoint de skill con verificación de firma de Amazon. Enruta cada intent al servicio correspondiente por prefijo (`roomba_order_`, `light_order_`, `temperature_sensor_order_`, `computer_status_order_`).
- **Asistente conversacional**: el intent `ConversationIntent` pasa la pregunta a Gemini, mantiene el historial en la sesión de Alexa y devuelve respuestas pensadas para leerse en voz alta.
- **Roomba**: control directo por MQTT/TLS contra el robot (iniciar, pausar, reanudar y enviar a la base), limpieza por habitaciones o de la casa completa, e inicio con temporizador.
- **Limpieza automática**: si los sensores de presencia indican que la casa está vacía, la Roomba arranca sola una vez al día entre las 08:00 y las 21:00, siempre que no esté atascada ni ya en marcha.
- **Luces**: encendido y apagado publicando en topics MQTT.
- **Termostato**: subir o bajar grados por zona, enviando el cambio relativo por MQTT sin `retain`.
- **Ordenador**: encendido por Wake-on-LAN, apagado por SSH (`systemctl poweroff`) y consulta de estado por ping.
- **Sensores**: registro y actualización de sensores de presencia, lluvia y temperatura. El estado de lluvia caduca a los 5 minutos sin detección.
- **Notificaciones push** al móvil vía [ntfy](https://ntfy.sh).

## Stack

| | |
|---|---|
| Lenguaje | Python 3.10+ |
| API | FastAPI, Uvicorn, fastapi-utils (controladores basados en clases) |
| Persistencia | PostgreSQL, SQLAlchemy 2 (async) + asyncpg, migraciones propias |
| Integraciones | aiomqtt / paho-mqtt, httpx, cryptography |
| Configuración | pydantic-settings + `.env` |
| Calidad | mypy (con plugin de pydantic) y ruff |
| Testing de API | Colección [Bruno](https://www.usebruno.com/) en `bruno/` |

## Arquitectura

```mermaid
flowchart LR
    AX[Alexa] -->|POST /api/alexa| C
    SEN[Sensores / clientes] -->|X-Api-Key| C
    subgraph HomeService
        C[service/web_api<br/>controllers] --> UC[application/use_case]
        UC --> GW[infraestructure/gateway]
        UC --> REP[infraestructure/persistence]
        BG[background_tasks<br/>workers + jobs] --> REP
        BG --> GW
    end
    REP --> DB[(PostgreSQL)]
    GW -->|MQTT| MQ[Broker MQTT<br/>luces · termostato]
    GW -->|MQTT/TLS| RB[Roomba]
    GW -->|WoL · SSH · ping| PC[Ordenador]
    GW -->|HTTPS| GEM[Gemini]
    GW -->|HTTPS| NT[ntfy]
```

```
src/
├── service/web_api/        # main.py (app + lifespan) y controllers
├── application/            # use_case, interfaces, DTOs y eventos
├── domain/model/           # entidades SQLAlchemy y enums
├── infraestructure/
│   ├── gateway/            # Alexa, Gemini, MQTT, Roomba, luces, termostato, PC, ntfy
│   ├── persistence/        # contexto, repositorios, DI y migraciones
│   └── background_tasks/   # PresenceSensorMonitor, RainSensorMonitor, RoombaActivationHandler
├── transversal/            # configuración, seguridad, utils, wrappers de respuesta, contratos JSON
└── AuxiliaryApplication/   # script de consola para probar la Roomba a mano
```

Al arrancar, el `lifespan` de FastAPI aplica las migraciones pendientes (tabla `database_version`) y lanza las tareas en segundo plano. Al apagar, las detiene de forma ordenada.

## Seguridad

- Todos los endpoints salvo `/api/alexa` exigen la cabecera `X-Api-Key`, comparada en tiempo constante. Si no hay clave configurada, el servidor rechaza la petición en lugar de dejarla pasar.
- `/api/alexa` valida la cadena de certificados, la firma y el timestamp de Amazon, además del skill ID.
- `X-Debug-Key` permite saltarse la API key en pruebas manuales. Solo funciona si `DEBUG_BYPASS_KEY` está definida.
- Los comandos del sistema (`ping`, `ssh`) se lanzan sin shell y con IP y usuario validados.
- Los secretos van en `.env` o en variables de entorno y nunca se versionan.

## Puesta en marcha

### Requisitos

- Python 3.10+
- PostgreSQL con la base de datos ya creada (las tablas las crean las migraciones)
- Broker MQTT para luces y termostato
- Para el control del ordenador:
  - Wake-on-LAN activado en el equipo.
  - Clave SSH en `~/.ssh/homelab_key`.
  - Un usuario con `sudo systemctl poweroff` sin contraseña.
- Opcional: skill de Alexa, API key de Gemini, topic de ntfy y credenciales locales de la Roomba

### Instalación

```bash
git clone https://github.com/marcosremon/homeservice.git
cd homeservice
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
```

### Configuración (`.env`)

| Variable | Descripción |
|---|---|
| `APP_HOST` / `APP_PORT` | Dirección de escucha (por defecto `0.0.0.0:5131`) |
| `DATABASE_URL` | `postgresql+asyncpg://usuario:password@host:5432/base_de_datos` |
| `INTERNAL_API_KEY` | Clave para `X-Api-Key`. Genera una con `python -c "import secrets; print(secrets.token_hex(32))"` |
| `DEBUG_BYPASS_KEY` | Clave para `X-Debug-Key` (déjala vacía en producción) |
| `ALEXA_SKILL_ID` / `ALEXA_VERSION` | ID de la skill en la consola de Amazon |
| `GEMINI_API_KEY` / `GEMINI_MODEL` | Credenciales de Google AI Studio (por defecto `gemini-2.5-flash`) |
| `ROOMBA_ID`, `ROOMBA_PORT`, `ROOMBA_BLID`, `ROOMBA_PASSWD`, `ROOMBA_PMAP_ID`, `ROOMBA_PMAP_VERSION` | Conexión local con la Roomba y mapa |
| `MQTT_HOST`, `MQTT_PORT`, `MQTT_USER`, `MQTT_PASSWORD` | Broker MQTT |
| `NTFY_BASE_URL` / `NTFY_TOPIC` | Notificaciones push (sin topic se desactivan) |
| `LAN_BROADCAST_IP`, `LAN_COMPUTER_IP`, `LAN_COMPUTER_MAC`, `LAN_CACHYOS_USER` | Wake-on-LAN y apagado por SSH |
| `LOG_LEVEL` | Nivel de log |

En producción no hace falta `.env`: las variables de entorno del contenedor o servicio tienen prioridad.

### Ejecutar

```bash
cd src
python -m service.web_api.main
```

La documentación interactiva queda en `http://localhost:5131/docs`.

### Probar la Roomba desde consola

```bash
cd src
python AuxiliaryApplication/Main.py start   # start | pause | resume | home | phase | rooms | raw
```

Matar el proceso no para la Roomba, porque la orden ya se ha publicado. Para detenerla, lanza `pause` o `home`.

### Comprobaciones

```bash
mypy
ruff check src
```

## Endpoints

Todas las rutas llevan el prefijo `/api`.

| Método  | Ruta | Descripción |
|---------|---|---|
| `POST`  | `/alexa` | Entrada de la skill de Alexa |
| `GET`   | `/change-computer-status/turn-on-computer` | Wake-on-LAN |
| `GET`   | `/change-computer-status/turn-off-computer` | Apagado por SSH |
| `GET`   | `/change-computer-status/get-computer-status` | Estado por ping |
| `POST`  | `/roomba/create-roomba` | Registrar Roomba |
| `PATCH`  | `/roomba/patch-roomba-state` | Actualizar estado |
| `POST`  | `/sensor/presence-sensor/create-presence-sensor` | Registrar sensor de presencia |
| `PATCH`  | `/sensor/presence-sensor/patch-presence-sensor-data` | Enviar lectura de presencia |
| `POST`  | `/sensors/temperature-sensor/create-temperature-sensor` | Registrar sensor de temperatura |
| `PATCH`  | `/sensors/temperature-sensor/patch-temperature-sensor` | Enviar lectura de temperatura |
| `POST`  | `/sensors/rain-sensor/create-rain-sensor` | Registrar sensor de lluvia |
| `PATCH` | `/sensors/rain-sensor/patch-rain-sensor` | Enviar detección de lluvia |

La colección de Bruno (`bruno/`) usa el entorno `Local` (`http://127.0.0.1:5131`) con `apiKey`, `debugKey` y `alexaSkillId` como variables secretas.

## Topics MQTT

| Topic | Payload |
|---|---|
| `home/<zona>/lights/cmd` | `ON` / `OFF` (o el `mqtt_topic` propio de la luz si está definido) |
| `home/<zona>/thermostat/cmd` | Incremento relativo, p. ej. `+2` / `-1` |

## Autor

**Marcos** — [@marcosremon](https://github.com/marcosremon)
