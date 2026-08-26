"""Equivalente a ApplicationDbContext + AddDbContext(UseNpgsql(...)) de Program.cs.

En SQLAlchemy no hay una clase DbContext: el "unit of work" es AsyncSession y la
configuracion del proveedor es el engine. Lo unico que si hay que replicar a mano
es la lista de DbSet<T>, porque una entidad solo entra en Base.metadata cuando su
modulo se importa. Los imports de abajo son esa lista.
"""

from collections.abc import AsyncIterator
from functools import lru_cache

from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine

from domain.model.entity.base import Base  # noqa: F401
from transversal.common.configuration.settings import get_settings

# region DbSet<T>
# Estos imports no se usan en el codigo de abajo: estan por su efecto colateral.
# Al importarse el modulo, la entidad se registra en Base.metadata.
# Sin esto, las claves ajenas no resuelven y Alembic no ve las tablas. El noqa
# evita que el linter los borre por "no usados".
from domain.model.entity.device import Device  # noqa: F401
from domain.model.entity.house_zone import HouseZone  # noqa: F401
from domain.model.entity.light import Light  # noqa: F401
from domain.model.entity.presence_sensor import PresenceSensor  # noqa: F401
from domain.model.entity.roomba import Roomba  # noqa: F401
# endregion

# region _get_session_factory
@lru_cache
def _get_session_factory() -> async_sessionmaker[AsyncSession]:
    """Perezoso: el engine se crea en la primera peticion, no al importar el modulo.

    Asi el .env ya esta cargado y los tests pueden sobreescribir la configuracion.
    """
    engine = create_async_engine(get_settings().database_url, echo=False, pool_pre_ping=True)
    return async_sessionmaker(engine, expire_on_commit=False)


async def get_session() -> AsyncIterator[AsyncSession]:
    """Una sesion por peticion, igual que el DbContext scoped de ASP.NET.

    El yield es lo que hace que FastAPI cierre la sesion al terminar la peticion,
    pase lo que pase.
    """
    async with _get_session_factory()() as session:
        yield session
# endregion