using HomeService.Transversal.Common.DatabaseMigration;

namespace HomeService.Infraestructure.Persistence.CreateDatabase.Migrations;

public static class Migration2026072802
{
    public static Migration Create() => new()
    {
        Version = 2026072802,
        Commands =
        [
            """
            ALTER TABLE public.device
                ADD COLUMN IF NOT EXISTS model TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS manufacturer TEXT NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS mac_address TEXT NOT NULL DEFAULT '';
            """,
        ],
    };
}
