using HomeService.Transversal.Common.DatabaseMigration;

namespace HomeService.Infraestructure.Persistence.CreateDatabase.Migrations;

public static class Migration2026083000
{
    public static Migration Create() => new Migration()
    {
        Version = 2026083000,
        Commands =
        [
            """
            CREATE TABLE IF NOT EXISTS public.rain_sensor (
                rain_sensor_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                adc_value INTEGER NOT NULL DEFAULT 4095,
                wetness_percent INTEGER NOT NULL DEFAULT 0,
                is_raining BOOLEAN NOT NULL DEFAULT FALSE,
                measure_at TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00',
                last_detected_rain TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
            );
            """,
        ],
    };
}
