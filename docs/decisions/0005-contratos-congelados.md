# 0005 — Rutas, claves JSON e intents congelados

**Estado**: aceptada

## Contexto

Las rutas HTTP las llama el firmware de los ESP32, y Alexa manda nombres de intent definidos en la consola de Amazon. Cambiarlos desde aquí no los cambia allí.

## Decisión

Se conservan tal cual estaban en el proyecto Python, incluidas sus erratas:

- **Rutas**: todas bajo `/api` (`RoutePrefixConvention` en `Program.cs`). Presencia cuelga de `sensor/` en singular y lluvia y temperatura de `sensors/`. Encender y apagar el ordenador son `GET`.
- **Claves JSON** de entrada y salida, fijadas con `[JsonPropertyName]` en `Transversal.JsonInterchange`. Presencia y roomba usan `callout` y lluvia y temperatura `callOut`; el estado del ordenador sale como `ComputerStatus`.
- **Intents**: los miembros de `IntentName` y `AlexaRequestType` tienen el nombre exacto que manda Amazon, y los services comparan los nombres completos (`light_order_encender_luces_salon`).

## Consecuencias

- Cambiar una ruta o una clave JSON obliga a reflashear los dispositivos; cambiar un intent, a tocar la skill.
- Lo interno sí se puede renombrar: los DTO de Application, por ejemplo, usan `CallOut` en todos los casos.
