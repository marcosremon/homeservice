from transversal.common.database_migration.Migration import Migration

def Migration2026072802DeviceMetadata() -> Migration:
    return Migration(
        version=2026072802,
        commands=[
            """CREATE TABLE IF NOT EXISTS public.temperature_sensor (
                temperature_sensor_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                temperature DOUBLE PRECISION NOT NULL DEFAULT 0.0,
                adc_voltage DOUBLE PRECISION NOT NULL DEFAULT 0.0,
                measure_at DOUBLE PRECISION NOT NULL DEFAULT 0.0
            );""",
        ],
    )