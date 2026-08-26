from transversal.common.database_migration.Migration import Migration

def Migration2026081800LightAndRoombaState() -> Migration:
    return Migration(
        version=2026081800,
        commands=[
            """ALTER TABLE public.light
                ADD COLUMN IF NOT EXISTS location TEXT NOT NULL DEFAULT 'NONE',
                ADD COLUMN IF NOT EXISTS mqtt_topic TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS last_status_change TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00',
                ADD COLUMN IF NOT EXISTS is_online BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS last_seen TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00';""",
            """UPDATE public.light l
                SET location = d.device_name
                FROM public.device d
                WHERE d.device_id = l.device_id
                  AND l.location = 'NONE'
                  AND d.device_name IN (
                    'MAIN', 'LONG_L', 'SHORT_L', 'LAOUNDRY_ROOM', 'LIVING_ROOM', 'GRANDMOTHER',
                    'KITCHEN', 'MARCOS', 'BATHROOM', 'DIEGO', 'DADS_CONECTOR', 'DADS', 'DADS_BATHROOM'
                  );""",
            """UPDATE public.light
                SET mqtt_topic = 'home/' || lower(location) || '/lights/cmd'
                WHERE mqtt_topic = '' AND location <> 'NONE';""",
            """ALTER TABLE public.roomba
                ADD COLUMN IF NOT EXISTS phase TEXT NOT NULL DEFAULT 'STOP',
                ADD COLUMN IF NOT EXISTS battery_percent INTEGER NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS bin_full BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS last_target TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS last_roomba_end TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00',
                ADD COLUMN IF NOT EXISTS last_clean_duration_minutes INTEGER NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS error_code INTEGER NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS error_message TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS pmap_id TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS user_pmapv_id TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS is_online BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS last_seen TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00';""",
        ],
    )