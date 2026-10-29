using HomeService.Transversal.Common.DatabaseMigration;

namespace HomeService.Infraestructure.Persistence.CreateDatabase.Migrations;

public static class Migration2026082700
{
    public static Migration Create() => new Migration()
    {
        Version = 2026082700,
        Commands =
        [
            """
            CREATE TABLE IF NOT EXISTS public.temperature_sensor (
                temperature_sensor_id BIGSERIAL PRIMARY KEY,
                device_id BIGINT NOT NULL REFERENCES public.device(device_id) ON DELETE CASCADE,
                temperature_celsius DOUBLE PRECISION NOT NULL DEFAULT 0.0,
                adc_voltage DOUBLE PRECISION NOT NULL DEFAULT 0.0,
                measure_at DOUBLE PRECISION NOT NULL DEFAULT 0.0
            );
            """,
        ],
    };
}
