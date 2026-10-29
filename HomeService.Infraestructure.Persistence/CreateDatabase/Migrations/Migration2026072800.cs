using HomeService.Transversal.Common.DatabaseMigration;

namespace HomeService.Infraestructure.Persistence.CreateDatabase.Migrations;

public static class Migration2026072800
{
    public static Migration Create() => new Migration()
    {
        Version = 2026072800,
        Commands =
        [
            """
            CREATE TABLE IF NOT EXISTS public.house_zone (
                house_zone_id BIGSERIAL PRIMARY KEY,
                callout TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS public.device (
                device_id BIGSERIAL PRIMARY KEY,
                house_zone_id BIGINT NOT NULL REFERENCES public.house_zone(house_zone_id) ON DELETE CASCADE,
                device_type TEXT NOT NULL,
                device_name TEXT NOT NULL
            );
            """,
        ],
    };
}
