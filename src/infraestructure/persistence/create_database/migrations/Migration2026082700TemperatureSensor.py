from transversal.common.database_migration.Migration import Migration

def Migration2026082700TemperatureSensor() -> Migration:
    return Migration(
        version=2026082700,
        commands=[
            """CREATE TABLE IF NOT EXISTS public.temperature_sensor (
                temperature_sensor_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                temperature_celsius DOUBLE PRECISION NOT NULL DEFAULT 0.0,
                adc_voltage DOUBLE PRECISION NOT NULL DEFAULT 0.0,
                measure_at DOUBLE PRECISION NOT NULL DEFAULT 0.0
            );""",
        ],
    )
