from transversal.common.database_migration.Migration import Migration

def Migration2026072801() -> Migration:
    return Migration(
        version=2026072801,
        commands=[
            """CREATE TABLE IF NOT EXISTS public.presence_sensor (
                presence_sensor_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                ts INTEGER NOT NULL,
                presence BOOLEAN NOT NULL,
                distance_cm INTEGER NOT NULL,
                motion TEXT NOT NULL,
                last_detected_presence TIMESTAMP
            );""",
            """CREATE TABLE IF NOT EXISTS public.roomba (
                roomba_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                last_roomba_activation TIMESTAMP NOT NULL
            );""",
            """CREATE TABLE IF NOT EXISTS public.light (
                light_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                is_on BOOLEAN NOT NULL,
                brightness INTEGER NOT NULL,
                color TEXT NOT NULL,
                color_temperature INTEGER NOT NULL
            );""",
        ],
    )