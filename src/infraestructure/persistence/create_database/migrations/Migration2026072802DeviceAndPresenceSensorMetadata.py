from transversal.common.database_migration.Migration import Migration

def Migration2026072802DeviceMetadata() -> Migration:
    return Migration(
        version=2026072802,
        commands=[
            """ALTER TABLE public.device
                ADD COLUMN IF NOT EXISTS model TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS manufacturer TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS mac_address TEXT NOT NULL DEFAULT '';""",
        ],
    )