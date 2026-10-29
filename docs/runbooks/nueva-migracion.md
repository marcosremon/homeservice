# Añadir una migración

El esquema lo gestiona `DatabaseMigrator`, no EF Core: ver [0003](../decisions/0003-esquema-databasemigrator.md).

## Pasos

1. Crea `HomeService.Infraestructure.Persistence/CreateDatabase/Migrations/MigrationYYYYMMDDNN.cs`, con la fecha del día y un contador de dos cifras:

   ```csharp
   using HomeService.Transversal.Common.DatabaseMigration;

   namespace HomeService.Infraestructure.Persistence.CreateDatabase.Migrations;

   public static class Migration2026110100
   {
       public static Migration Create() => new()
       {
           Version = 2026110100,
           Commands =
           [
               """
               ALTER TABLE public.light
                   ADD COLUMN IF NOT EXISTS ejemplo TEXT NOT NULL DEFAULT '';
               """,
           ],
       };
   }
   ```

2. Añádela a `_postgreSqlMigrations` en `CreateDatabase/DatabaseMigrator.PostgreSqlMigrations.cs`. El orden de la lista da igual: se aplican ordenadas por `Version`.
3. Si añade o cambia columnas, actualiza la entidad en `Domain.Model/Entity` y su mapeo en `ApplicationDbContext.OnModelCreating`.
4. Pruébala contra una base de datos de pruebas, nunca primero contra la real. En consola debe salir `DatabaseMigrator -> 1 migracion(es) aplicada(s). Version X -> Y.`

## Reglas

- **No edites una migración ya aplicada**: su versión ya está en `database_version` y no volvería a ejecutarse. Corrige con una nueva.
- Usa `IF NOT EXISTS` e `IF EXISTS`, como las existentes.
- Fechas en `TIMESTAMP` (sin zona horaria) y con default `'0001-01-01 00:00:00'`, como el resto del esquema.
- Nombres de tabla y columna en snake_case.
- `Version` es un `int`: el formato `YYYYMMDDNN` cabe hasta el año 2147.
