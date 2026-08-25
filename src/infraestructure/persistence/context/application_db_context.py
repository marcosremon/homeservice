"""Equivalente a ApplicationDbContext + AddDbContext(UseNpgsql(...)) de Program.cs."""

import os
from collections.abc import AsyncIterator

from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine

# postgresql+asyncpg://usuario:password@host:puerto/base_de_datos
DATABASE_URL = os.getenv("DATABASE_URL", "")

_engine = create_async_engine(DATABASE_URL, echo=False, pool_pre_ping=True) if DATABASE_URL else None
_session_factory = async_sessionmaker(_engine, expire_on_commit=False) if _engine else None


async def get_session() -> AsyncIterator[AsyncSession]:
    """Una sesion por peticion, igual que el DbContext scoped de ASP.NET.

    El yield es lo que hace que FastAPI cierre la sesion al terminar la peticion,
    pase lo que pase.
    """
    if _session_factory is None:
        raise RuntimeError("DATABASE_URL no configurada.")

    async with _session_factory() as session:
        yield session
