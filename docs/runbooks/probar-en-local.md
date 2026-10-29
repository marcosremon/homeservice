# Probar en local sin tocar la casa

Con el `.env` real, arrancar la app **actúa sobre cosas de verdad**:

- El migrador ejecuta DDL contra la base de datos real.
- Las órdenes de Alexa publican en el broker MQTT (luces, termostatos), mueven la roomba o mandan notificaciones por ntfy.
- `PresenceSensorMonitor` arranca la roomba si la casa está vacía entre las 8:00 y las 21:00. Hoy no ocurre porque el repositorio es esqueleto, pero ocurrirá en cuanto tenga lógica.

Las variables de entorno tienen prioridad sobre el `.env`, así que basta con apuntar todo lo externo a puertos cerrados de `127.0.0.1`.

## Arrancar

Desde Git Bash, en `HomeService.Service.WebApi`, después de compilar:

```bash
export DATABASE_URL="Host=127.0.0.1;Port=1;Database=smoke;Username=smoke;Password=smoke"
export INTERNAL_API_KEY=smoke DEBUG_BYPASS_KEY=smokebypass
export MQTT_HOST=127.0.0.1 MQTT_PORT=1 ROOMBA_ID=127.0.0.1 ROOMBA_PORT=1
export NTFY_BASE_URL=http://127.0.0.1:1 GEMINI_API_KEY=smoke
export LAN_COMPUTER_IP=127.0.0.1 LAN_COMPUTER_MAC=00:00:5E:00:53:01 LAN_BROADCAST_IP=127.0.0.1 LAN_CACHYOS_USER=smoke
export ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5099
dotnet bin/Debug/net10.0/HomeService.Service.WebApi.dll
```

- El migrador tarda unos 20 segundos en rendirse (10 reintentos cada 2 s); después la app sigue arrancando.
- Usa valores falsos pero no vacíos, para no depender de cómo trate el sistema una variable vacía: si no llegara al proceso, ganaría el valor real del `.env`.
- Se ejecuta el `.dll` directamente, no `dotnet run`, para que al pararlo no quede un proceso hijo escuchando.

## Peticiones

- Rutas publicadas: `GET http://127.0.0.1:5099/openapi/v1.json`
- REST: cabecera `X-Api-Key: smoke`.
- Alexa sin firma de Amazon: cabecera `X-Debug-Key: smokebypass`, que se salta la firma y la comprobación del skill id.

```bash
curl -X POST http://127.0.0.1:5099/api/alexa -H "Content-Type: application/json" -H "X-Debug-Key: smokebypass" -d '{"version":"1.0","request":{"type":"LaunchRequest","requestId":"r","timestamp":"2026-10-29T10:00:00Z"}}'
```

Para un intent, cambia `request` por `{"type":"IntentRequest","requestId":"r","timestamp":"...","intent":{"name":"light_order_encender_luces_salon","confirmationStatus":"NONE"}}`.

**No pruebes `ConversationIntent`**: llama a Gemini, que es un servicio externo de verdad.

## El ordenador

`/api/change-computer-status` habla de verdad por la LAN: ping, Wake on LAN y SSH con `sudo systemctl poweroff`. Las cuatro variables `LAN_*` del bloque de arriba son las que lo mantienen lejos de tu PC; no quites ninguna.

- `LAN_COMPUTER_IP=127.0.0.1` simula un ordenador **encendido**: encender responde «PC already on» y apagar intenta un SSH a este equipo, que se rechaza si no hay nada en el puerto 22.
- `LAN_COMPUTER_IP=192.0.2.1` (IP de documentación, no existe) simula uno **apagado**: apagar no lanza SSH y encender manda el paquete mágico, que con `LAN_BROADCAST_IP=127.0.0.1` y la MAC de documentación no sale de este equipo. Se puede capturar escuchando UDP en `127.0.0.1:9`.

`Settings` se lee una vez por proceso: para cambiar de escenario hay que reiniciar la app.

## Con una base de datos de verdad, desechable

Si el equipo tiene PostgreSQL instalado (aquí, `C:\Program Files\PostgreSQL\17`), se puede levantar un cluster temporal en otro puerto sin tocar ninguna instancia existente. `DatabaseMigrator` crea el esquema desde cero al arrancar la app.

```bash
PG="/c/Program Files/PostgreSQL/17/bin"; DATA="C:/temp/homeservice-pgdata"
"$PG/initdb" -D "$DATA" -U smoke --auth=trust -E UTF8 --locale=C
"$PG/pg_ctl" -D "$DATA" -o "-p 55432 -c listen_addresses=127.0.0.1" -l "$DATA.log" -w start
"$PG/createdb" -h 127.0.0.1 -p 55432 -U smoke homeservice_smoke
export DATABASE_URL="Host=127.0.0.1;Port=55432;Database=homeservice_smoke;Username=smoke;Password=smoke"
```

Después, el resto de variables del bloque de arriba y la app. Al terminar: `"$PG/pg_ctl" -D "$DATA" -m fast stop` y borrar `$DATA`.

- Las luces no tienen endpoint de alta: se insertan a mano en `device` (con `device_type = 'light'`) y en `light`.
- `RainSensorMonitor` pasa cada 60 s. Para probar que la lluvia caduca, pon `last_detected_rain` a hace más de 5 minutos (en UTC).

## Qué esperar hoy

| Petición | Resultado |
|---|---|
| Ruta sin `/api` | 404 |
| REST sin clave o con clave mala | 401 |
| REST con clave buena | 500 con `responseCodeJson: 998`: sin base de datos, el repositorio falla al conectar. `/api/change-computer-status` sí responde, porque no usa la BBDD |
| Alexa sin bypass ni firma | 401 |
| `LaunchRequest` | 200, saludo con la sesión abierta |
| `light_order_encender_luces_salon` | «No encuentro la luz de LIVING_ROOM» |
| `temperature_sensor_order_subir_temperatura` | «Hubo un error al cambiar la temperatura»: MQTT cerrado |
| `computer_status_order_get_status` | «El ordenador está encendido.»: ping a 127.0.0.1 |
