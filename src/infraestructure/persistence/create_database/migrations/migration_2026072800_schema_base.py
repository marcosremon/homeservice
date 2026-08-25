from transversal.common.database_migration.migration import Migration

def migration_2026072800_schema_base() -> Migration:
    return Migration(
        version=2026072800,
        commands=[
            """CREATE TABLE IF NOT EXISTS public.house_zone (
                house_zone_id BIGSERIAL PRIMARY KEY,
                callout TEXT NOT NULL
            );""",
            """CREATE TABLE IF NOT EXISTS public.device (
                device_id BIGSERIAL PRIMARY KEY,
                house_zone_id BIGINT NOT NULL REFERENCES public.house_zone(house_zone_id) ON DELETE CASCADE,
                device_type TEXT NOT NULL,
                device_name TEXT NOT NULL
            );""",
        ],
    )