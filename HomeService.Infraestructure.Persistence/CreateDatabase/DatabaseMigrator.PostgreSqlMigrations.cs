using HomeService.Infraestructure.Persistence.CreateDatabase.Migrations;
using HomeService.Transversal.Common.DatabaseMigration;

namespace HomeService.Infraestructure.Persistence.CreateDatabase
{
    public static partial class DatabaseMigrator
    {
        #region _postgreSqlMigrations
        // El orden de la lista es irrelevante: _getPendingMigrations ordena por Version.
        private static readonly List<Migration> _postgreSqlMigrations =
        [
            Migration2026072800.Create(),
            Migration2026072801.Create(),
            Migration2026072802.Create(),
            Migration2026081800.Create(),
            Migration2026082700.Create(),
            Migration2026083000.Create(),
        ];
        #endregion
    }
}
