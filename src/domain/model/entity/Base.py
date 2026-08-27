from sqlalchemy.orm import DeclarativeBase

class Base(DeclarativeBase):
    """Raiz de todas las entidades mapeadas.

    No tiene equivalente en C#: alli ApplicationDbContext declara los DbSet<T> y
    las entidades quedan como POCOs. En SQLAlchemy el mapeo se construye al crear
    la clase, asi que cada entidad tiene que heredar de aqui.
    """