# HomeService

API de domótica en ASP.NET Core (.NET 10): skill de Alexa, roomba, luces y termostatos por MQTT, sensores ESP32 de presencia, lluvia y temperatura, y control del ordenador por la LAN. Es el port a C# de `../homeservicePy` (FastAPI), que sirve de referencia de **comportamiento**, no de estructura.

## Capas y referencias

| Proyecto | Referencia a |
|---|---|
| `Domain.Model`, `Transversal.Wrapper` | nada |
| `Application.DataTransferObject` | Domain.Model, Transversal.Wrapper |
| `Transversal.JsonInterchange` | Transversal.Wrapper |
| `Application.Event`, `Application.Interface` | Application.DataTransferObject |
| `Application.UseCase` | Application.Interface, Application.DataTransferObject |
| `Transversal.Common` | Application.DataTransferObject, Application.Event, Domain.Model, Transversal.JsonInterchange, Transversal.Wrapper |
| `Transversal.Security` | Application.Interface, Transversal.Common |
| `Infraestructure.Gateway` | Application.Interface, Application.DataTransferObject, Domain.Model, Transversal.Common, Transversal.Wrapper |
| `Infraestructure.BackgroundTasks` | Application.Interface, Application.Event, Application.DataTransferObject, Transversal.Common |
| `Infraestructure.Persistence` | Application.Interface, Application.UseCase, Domain.Model, Infraestructure.Gateway, Infraestructure.BackgroundTasks, Transversal.Common, Transversal.Security |
| `Service.WebApi` | Infraestructure.Persistence, Transversal.Common, Transversal.Security |

Reglas que no se ven leyendo el código:
- **Solo `Service.WebApi` referencia `Infraestructure.Persistence`.** Persistence registra la DI de toda la solución (`AddInfrastructureServices`), así que si Gateway, BackgroundTasks o Security la referenciaran habría un ciclo. Se depende de interfaces de `Application.Interface`. → [0004](docs/decisions/0004-persistence-registra-todo.md)
- **`Transversal.Common` no es la capa base**: está por encima de DTO, Event y JsonInterchange. Lo que esas capas necesitan (`BaseResponse`, payloads de Alexa) va en `Transversal.Wrapper`. → [0001](docs/decisions/0001-transversal-wrapper.md)

## Convenciones de C#

- Seguir el estilo C# del autor, no la estructura del proyecto Python.
- Tipos explícitos en las declaraciones, sin `var`. `#region` por método.
- Privados en `_camelCase`, también métodos y constantes (`_runMigrations`, `_numberTableVersion`). Excepción heredada: `Transversal.Common` y `Transversal.Security` tienen privados en PascalCase.
- Namespace de bloque en `Infraestructure.*` y `Service.WebApi`. Domain, Application y Transversal usan namespace de fichero (`namespace X;`); respetar el del fichero que se toque.
- Async sin sufijo `Async` (`PatchRoombaState`, no `PatchRoombaStateAsync`).
- Miembros de enum con el nombre exacto del origen (`FULL_HOUSE`, `roomba_order_`): se buscan por nombre.
- `[JsonPropertyName]` solo en `Transversal.JsonInterchange` y en los payloads de Alexa; los DTO de Application son POCOs.
- En mensajes de error que llegan al cliente, `ex.Message`, nunca la excepción entera.

## Configuración

- Todo sale de `Settings.GetSettings()` (`Transversal.Common/Configuration/Settings.cs`), que lee el `.env` de la raíz; las variables de entorno tienen prioridad. **Nunca `IConfiguration` ni `GetConnectionString`.** → [0002](docs/decisions/0002-configuracion-settings-env.md)
- `DATABASE_URL` es una cadena de Npgsql (`Host=...;Port=...;...`), no una URL.
- `.env.example` es la plantilla sin secretos; `.env` está en `.gitignore`.

## Base de datos

El esquema lo crea `DatabaseMigrator` al arrancar, con SQL a mano; EF Core solo mapea. Para cambiarlo: [nueva migración](docs/runbooks/nueva-migracion.md). → [0003](docs/decisions/0003-esquema-databasemigrator.md)

## Contratos que no se tocan

Rutas HTTP, claves JSON e intents de Alexa: los usan los ESP32 y Amazon tal cual, erratas incluidas (`/api/sensor/presence-sensor` en singular, `callout` frente a `callOut`). → [0005](docs/decisions/0005-contratos-congelados.md)

## Compilar y probar

- `dotnet build HomeService.Service.WebApi.slnx`
- **No arrancar con el `.env` real para probar**: el migrador toca la BBDD y Alexa y los monitores actúan sobre dispositivos de verdad. → [probar en local](docs/runbooks/probar-en-local.md)

## Pendiente o roto

- `ApplicationDbContext`: los `DbSet` `Lights`, `PresenceSensors` y `RainSensors` tienen el tipo cruzado (`RainSensor` no está en el modelo) y `OnModelCreating` está vacío, así que EF buscaría `HouseZones` en vez de `house_zone`.
- Los 6 repositorios son esqueleto (`try` vacío, `catch` y `return`): devuelven `IsSuccess = false` y código 998, que los endpoints REST convierten en un 500.
- `LightController` sin portar: el Python estaba a medias e `ILightApplication` no tiene métodos.
- «Enciende/apaga el ordenador» por voz no hace nada: `ComputerStatusService` solo consulta el estado. El encendido real está en `/api/change-computer-status`.
