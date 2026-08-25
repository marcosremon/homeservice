from transversal.common.database_migration.migration import Migration

def migration_2026072802_device_metadata() -> Migration:
    return Migration(
        version=2026072802,
        commands=[
            """ALTER TABLE public.device
                ADD COLUMN IF NOT EXISTS model TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS manufacturer TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS mac_address TEXT NOT NULL DEFAULT '';""",
        ],
    )