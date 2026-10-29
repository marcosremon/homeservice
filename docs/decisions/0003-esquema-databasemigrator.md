# 0003 — El esquema lo gestiona `DatabaseMigrator`; EF Core solo mapea

**Estado**: aceptada

## Contexto

La base de datos real ya existe, creada por un migrador propio con SQL versionado y una tabla `database_version`. Tablas y columnas están en snake_case.

## Decisión

- `DatabaseMigrator` (`Infraestructure.Persistence/CreateDatabase`, partial y síncrono) se ejecuta al arrancar, antes de servir. Aplica en **una sola transacción** todas las migraciones con versión mayor que la guardada en `database_version`.
- Cada migración es un fichero `Migrations/MigrationYYYYMMDDNN.cs`, registrado en `_postgreSqlMigrations` (`DatabaseMigrator.PostgreSqlMigrations.cs`).
- EF Core no crea ni migra nada: no hay migraciones de EF ni `EnsureCreated`. `ApplicationDbContext` solo describe el mapeo.

## Consecuencias

- El mapeo hay que declararlo a mano en `OnModelCreating`: las convenciones de EF darían `HouseZones` y `HouseZoneId`, no `house_zone` y `house_zone_id`.
- Única columna cuyo nombre no es el snake_case de su propiedad: `TemperatureSensor.Temperature` → `temperature_sensor.temperature_celsius`.
- Las fechas son `timestamp without time zone`, y Npgsql rechaza escribir en ellas un `DateTime` con `Kind = Utc`. Por eso `GeneralUtils.UtcNow()` devuelve `Kind = Unspecified`.
- La fecha «vacía» es `'0001-01-01 00:00:00'`, el default de las migraciones. Npgsql, por defecto, guarda `DateTime.MinValue` como `-infinity`; `Program.cs` lo desactiva con `AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true)` para no mezclar dos representaciones en la misma columna.
- Desajustes conocidos entre entidades y esquema:
  - `presence_sensor.ts` es `INTEGER` y `PresenceSensor.Ts` es `long`: el valor tiene que caber en int32.
  - `presence_sensor.last_detected_presence` admite `NULL` y la propiedad no es anulable.
- Si el migrador falla, la app arranca igual y solo lo avisa por consola.

Para añadir una: [nueva migración](../runbooks/nueva-migracion.md).
