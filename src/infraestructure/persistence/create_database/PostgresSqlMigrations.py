from infraestructure.persistence.create_database.migrations.Migration2026072800 import Migration2026072800
from infraestructure.persistence.create_database.migrations.Migration2026072801 import Migration2026072801
from infraestructure.persistence.create_database.migrations.Migration2026072802 import Migration2026072802
from infraestructure.persistence.create_database.migrations.Migration2026081800 import Migration2026081800
from infraestructure.persistence.create_database.migrations.Migration2026082700 import Migration2026082700
from infraestructure.persistence.create_database.migrations.Migration2026083000 import Migration2026083000
from transversal.common.database_migration.Migration import Migration

class PostgresSqlMigrations:

    @staticmethod
    def GetPostgresSqlMigrations() -> list[Migration]:
        return [
            Migration2026072800(),
            Migration2026072801(),
            Migration2026072802(),
            Migration2026081800(),
            Migration2026082700(),
            Migration2026083000(),
        ]