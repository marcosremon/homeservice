import asyncio
from typing import Any
from sqlalchemy import CursorResult, text
from sqlalchemy.ext.asyncio import AsyncConnection, AsyncEngine, create_async_engine
from infraestructure.persistence.create_database.PostgresSqlMigrations import PostgresSqlMigrations
from transversal.common.configuration.Settings import GetSettings
from transversal.common.database_migration.Migration import Migration

class DatabaseMigrator:
    MAXIMUM_NUMBER_OF_CONNECTION_RETRIES: int = 10
    TIME_WAIT_RETRY_SECONDS: float = 2.0
    _NUMBER_TABLE_VERSION: str = "database_version"

    # region create_or_update_database
    @classmethod
    async def CreateOrUpdateDatabase(cls) -> bool:
        return await cls._runMigrations(GetSettings().databaseUrl, PostgresSqlMigrations.POSTGRESQL_MIGRATIONS)
    # endregion

    # region _get_pending_migrations
    @staticmethod
    def _getPendingMigrations(actualVersion: int, allVersions: list[Migration]) -> list[Migration]:
        return sorted((m for m in allVersions if m.version > actualVersion), key=lambda m: m.version)
    # endregion

    # region _run_migrations
    @classmethod
    async def _runMigrations(cls, connectionString: str, migrations: list[Migration]) -> bool:
        if not connectionString.strip():
            cls._log("ERROR: cadena de conexion vacia.")
            return False

        engine: AsyncEngine = create_async_engine(connectionString)
        try:
            connection: AsyncConnection = await cls._openWithRetry(engine)
            async with connection:
                exists: CursorResult[Any] = await connection.exec_driver_sql(
                    "SELECT count(*) FROM information_schema.tables "
                    f"WHERE table_schema = 'public' AND table_name = '{cls._NUMBER_TABLE_VERSION}';"
                )
                if exists.scalar_one() == 0:
                    try:
                        async with connection.begin():
                            await connection.exec_driver_sql(
                                f'CREATE TABLE public."{cls._NUMBER_TABLE_VERSION}" ("{cls._NUMBER_TABLE_VERSION}" integer NOT NULL);'
                            )
                            await connection.exec_driver_sql(
                                f'INSERT INTO public."{cls._NUMBER_TABLE_VERSION}" ("{cls._NUMBER_TABLE_VERSION}") VALUES (0);'
                            )
                        cls._log(f"tabla {cls._NUMBER_TABLE_VERSION} creada (version inicial 0).")
                    except Exception as ex:
                        # El async with begin() ya hizo rollback al salir por excepcion.
                        cls._log(f"ERROR creando la tabla de version: {ex}")
                        return False

                result: CursorResult[Any] = await connection.exec_driver_sql(
                    f'SELECT "{cls._NUMBER_TABLE_VERSION}" FROM public."{cls._NUMBER_TABLE_VERSION}";'
                )
                lastVersion: int = int(result.scalar_one())

                pending: list[Migration] = cls._getPendingMigrations(lastVersion, migrations)
                if not pending:
                    cls._log(f"BBDD al dia (version {lastVersion}).")
                    return True

                lastCommand: str = ""
                try:
                    async with connection.begin():
                        for migration in pending:
                            for sql in migration.commands:
                                lastCommand = sql
                                await connection.exec_driver_sql(sql)

                        newVersion: int = pending[-1].version
                        # text() en vez de exec_driver_sql: aqui si hay parametro, y asi
                        # no dependemos del paramstyle del driver (asyncpg usa $1, no %s).
                        await connection.execute(
                            text(f'UPDATE public."{cls._NUMBER_TABLE_VERSION}" SET "{cls._NUMBER_TABLE_VERSION}" = :new_version;'),
                            {"new_version": newVersion},
                        )

                    cls._log(f"{len(pending)} migracion(es) aplicada(s). Version {lastVersion} -> {newVersion}.")
                    return True
                except Exception as ex:
                    cls._log(f"ERROR aplicando migraciones. Ultimo comando: {lastCommand}\n{ex}")
                    return False
        except Exception as ex:
            cls._log(f"ERROR inesperado: {ex}")
            return False
        finally:
            await engine.dispose()
    # endregion

    # region _open_with_retry
    @classmethod
    async def _openWithRetry(cls, engine: AsyncEngine) -> AsyncConnection:
        """Espera a que Postgres acepte conexiones, igual que el bucle de C#."""
        attempt: int = 0
        while True:
            try:
                return await engine.connect()
            except Exception as ex:
                if attempt >= cls.MAXIMUM_NUMBER_OF_CONNECTION_RETRIES:
                    raise

                cls._log(f"reintentando conexion ({attempt + 1}/{cls.MAXIMUM_NUMBER_OF_CONNECTION_RETRIES}). {ex}")
                await asyncio.sleep(cls.TIME_WAIT_RETRY_SECONDS)
                attempt += 1
    # endregion

    # region _log
    @staticmethod
    def _log(message: str) -> None:
        print(f"DatabaseMigrator -> {message}")
    # endregion