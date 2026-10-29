# 0002 — Configuración por `Settings` y `.env`, sin `IConfiguration`

**Estado**: aceptada

## Contexto

El proyecto Python lee la configuración con pydantic `BaseSettings` desde un `.env`. El primer borrador en C# usaba `IConfiguration.GetConnectionString("PostgreSQLConnection")`, pero no hay bloque `ConnectionStrings` en ningún `appsettings`.

## Decisión

- `Settings` (`Transversal.Common/Configuration/Settings.cs`) es un POCO con `[ConfigurationKeyName("UPPER_SNAKE")]`, los mismos nombres de variable que el `.env` de Python.
- `Settings.GetSettings()` lo construye una sola vez (`Lazy<T>`): lee el `.env` buscándolo hacia arriba desde el binario y luego las variables de entorno, que tienen prioridad.
- `DATABASE_URL` e `INTERNAL_API_KEY` son obligatorias: si faltan, lanza.
- La DI registra esa misma instancia como singleton para inyectarla.

## Consecuencias

- Nunca `IConfiguration` ni `GetConnectionString` para valores de la app.
- `DATABASE_URL` es una cadena de Npgsql, no una URL de SQLAlchemy.
- Cualquier valor se sobrescribe con una variable de entorno, que es lo que usa [probar en local](../runbooks/probar-en-local.md).
- `APP_HOST` y `APP_PORT` existen en `Settings` pero no los lee nadie: Kestrel escucha lo que diga `launchSettings.json` o `ASPNETCORE_URLS`.
- `DatabaseMigrator.CreateOrUpdateDatabase()` llama a `GetSettings()` al evaluar su argumento: si falta una variable obligatoria, el arranque se cae con la excepción en vez de recibir `false`.
