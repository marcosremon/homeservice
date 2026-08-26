import asyncio

from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncConnection, create_async_engine

from infraestructure.persistence.create_database.PostgresqlMigrations import POSTGRESQL_MIGRATIONS
from transversal.common.configuration.Settings import GetSettings
from transversal.common.database_migration.Migration import Migration

MAXIMUM_NUMBER_OF_CONNECTION_RETRIES = 10
TIME_WAIT_RETRY_SECONDS = 2.0
_NUMBER_TABLE_VERSION = "database_version"

# region create_or_update_database
async def CreateOrUpdateDatabase() -> bool:
    return await _runMigrations(GetSettings().databaseUrl, POSTGRESQL_MIGRATIONS)
# endregion

# region _get_pending_migrations
def _getPendingMigrations(actualVersion: int, allVersions: list[Migration]) -> list[Migration]:
    return sorted((m for m in allVersions if m.version > actualVersion), key=lambda m: m.version)
# endregion

# region _run_migrations
async def _runMigrations(connectionString: str, migrations: list[Migration]) -> bool:
    if not connectionString.strip():
        _log("ERROR: cadena de conexion vacia.")
        return False

    engine = create_async_engine(connectionString)
    try:
        connection = await _openWithRetry(engine)
        async with connection:
            exists = await connection.exec_driver_sql(
                "SELECT count(*) FROM information_schema.tables "
                f"WHERE table_schema = 'public' AND table_name = '{_NUMBER_TABLE_VERSION}';"
            )
            if exists.scalar_one() == 0:
                try:
                    async with connection.begin():
                        await connection.exec_driver_sql(
                            f'CREATE TABLE public."{_NUMBER_TABLE_VERSION}" ("{_NUMBER_TABLE_VERSION}" integer NOT NULL);'
                        )
                        await connection.exec_driver_sql(
                            f'INSERT INTO public."{_NUMBER_TABLE_VERSION}" ("{_NUMBER_TABLE_VERSION}") VALUES (0);'
                        )
                    _log(f"tabla {_NUMBER_TABLE_VERSION} creada (version inicial 0).")
                except Exception as ex:
                    # El async with begin() ya hizo rollback al salir por excepcion.
                    _log(f"ERROR creando la tabla de version: {ex}")
                    return False

            result = await connection.exec_driver_sql(
                f'SELECT "{_NUMBER_TABLE_VERSION}" FROM public."{_NUMBER_TABLE_VERSION}";'
            )
            lastVersion = int(result.scalar_one())

            pending = _getPendingMigrations(lastVersion, migrations)
            if not pending:
                _log(f"BBDD al dia (version {lastVersion}).")
                return True

            lastCommand = ""
            try:
                async with connection.begin():
                    for migration in pending:
                        for sql in migration.commands:
                            lastCommand = sql
                            await connection.exec_driver_sql(sql)

                    newVersion = pending[-1].version
                    # text() en vez de exec_driver_sql: aqui si hay parametro, y asi
                    # no dependemos del paramstyle del driver (asyncpg usa $1, no %s).
                    await connection.execute(
                        text(f'UPDATE public."{_NUMBER_TABLE_VERSION}" SET "{_NUMBER_TABLE_VERSION}" = :new_version;'),
                        {"new_version": newVersion},
                    )

                _log(f"{len(pending)} migracion(es) aplicada(s). Version {lastVersion} -> {newVersion}.")
                return True
            except Exception as ex:
                _log(f"ERROR aplicando migraciones. Ultimo comando: {lastCommand}\n{ex}")
                return False
    except Exception as ex:
        _log(f"ERROR inesperado: {ex}")
        return False
    finally:
        await engine.dispose()
# endregion

# region _open_with_retry
async def _openWithRetry(engine) -> AsyncConnection:
    """Espera a que Postgres acepte conexiones, igual que el bucle de C#."""
    attempt = 0
    while True:
        try:
            return await engine.connect()
        except Exception as ex:
            if attempt >= MAXIMUM_NUMBER_OF_CONNECTION_RETRIES:
                raise

            _log(f"reintentando conexion ({attempt + 1}/{MAXIMUM_NUMBER_OF_CONNECTION_RETRIES}). {ex}")
            await asyncio.sleep(TIME_WAIT_RETRY_SECONDS)
            attempt += 1
# endregion

# region _log
def _log(message: str) -> None:
    print(f"DatabaseMigrator -> {message}")
# endregion