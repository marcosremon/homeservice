"""Equivalente a ApplicationDbContext + AddDbContext(UseNpgsql(...)) de Program.cs.

En SQLAlchemy no hay una clase DbContext: el "unit of work" es AsyncSession y la
configuracion del proveedor es el engine. Lo unico que si hay que replicar a mano
es la lista de DbSet<T>, porque una entidad solo entra en Base.metadata cuando su
modulo se importa. Los imports de abajo son esa lista.
"""

from collections.abc import AsyncIterator
from functools import lru_cache

from sqlalchemy.ext.asyncio import AsyncEngine, AsyncSession, async_sessionmaker, create_async_engine

from domain.model.entity.Base import Base  # noqa: F401
from transversal.common.configuration.Settings import GetSettings

# region DbSet<T>
# Estos imports no se usan en el codigo de abajo: estan por su efecto colateral.
# Al importarse el modulo, la entidad se registra en Base.metadata.
# Sin esto, las claves ajenas no resuelven y Alembic no ve las tablas. El noqa
# evita que el linter los borre por "no usados".
from domain.model.entity.Device import Device  # noqa: F401
from domain.model.entity.HouseZone import HouseZone  # noqa: F401
from domain.model.entity.Light import Light  # noqa: F401
from domain.model.entity.PresenceSensor import PresenceSensor  # noqa: F401
from domain.model.entity.RainSensor import RainSensor  # noqa: F401
from domain.model.entity.Roomba import Roomba  # noqa: F401
from domain.model.entity.TemperatureSensor import TemperatureSensor  # noqa: F401
# endregion

# region _get_session_factory
@lru_cache
def _getSessionFactory() -> async_sessionmaker[AsyncSession]:
    """Perezoso: el engine se crea en la primera peticion, no al importar el modulo.

    Asi el .env ya esta cargado y los tests pueden sobreescribir la configuracion.
    """
    engine: AsyncEngine = create_async_engine(GetSettings().databaseUrl, echo=False, pool_pre_ping=True)
    return async_sessionmaker(engine, expire_on_commit=False)

async def GetSession() -> AsyncIterator[AsyncSession]:
    """Una sesion por peticion, igual que el DbContext scoped de ASP.NET.

    El yield es lo que hace que FastAPI cierre la sesion al terminar la peticion,
    pase lo que pase.
    """
    async with _getSessionFactory()() as session:
        yield session
# endregion