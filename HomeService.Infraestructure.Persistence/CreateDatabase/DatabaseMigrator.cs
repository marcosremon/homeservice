using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.DatabaseMigration;
using Npgsql;
using System.Data;

namespace HomeService.Infraestructure.Persistence.CreateDatabase
{
    public static partial class DatabaseMigrator
    {
        public static int maximumNumberOfConnectionRetries = 10;
        public static int timeWaitRetryMs = 2000;
        private const string _numberTableVersion = "database_version";

        #region CreateOrUpdateDatabase
        public static bool CreateOrUpdateDatabase() => _runMigrations(Settings.GetSettings().DatabaseUrl, _postgreSqlMigrations);
        #endregion

        #region _getPendingMigrations
        private static List<Migration> _getPendingMigrations(int actualVersion, IEnumerable<Migration> allVersions) =>
            allVersions
            .Where(m => m.Version > actualVersion)
            .OrderBy(m => m.Version)
            .ToList();
        #endregion

        #region _runMigrations
        private static bool _runMigrations(string? connectionString, List<Migration> migrations)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _log("ERROR: cadena de conexion vacia.");
                return false;
            }

            try
            {
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                _openWithRetry(connection);

                string sqlCommand = $"SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{_numberTableVersion}';";
                using (NpgsqlCommand checkTable = new NpgsqlCommand(sqlCommand, connection))
                {
                    bool exist = Convert.ToInt64(checkTable.ExecuteScalar()) > 0;
                    if (!exist)
                    {
                        using NpgsqlTransaction txCreate = connection.BeginTransaction();
                        try
                        {
                            new NpgsqlCommand($"CREATE TABLE public.\"{_numberTableVersion}\" (\"{_numberTableVersion}\" integer NOT NULL);", connection, txCreate).ExecuteNonQuery();
                            new NpgsqlCommand($"INSERT INTO public.\"{_numberTableVersion}\" (\"{_numberTableVersion}\") VALUES (0);", connection, txCreate).ExecuteNonQuery();
                            txCreate.Commit();
                            _log($"tabla {_numberTableVersion} creada (version inicial 0).");
                        }
                        catch (Exception ex)
                        {
                            txCreate.Rollback();
                            _log("ERROR creando la tabla de version: " + ex);
                            return false;
                        }
                    }
                }

                int lastVersion;
                using (NpgsqlCommand readVersion = new NpgsqlCommand($"SELECT \"{_numberTableVersion}\" FROM public.\"{_numberTableVersion}\";", connection))
                    lastVersion = Convert.ToInt32(readVersion.ExecuteScalar());

                List<Migration> pending = _getPendingMigrations(lastVersion, migrations);
                if (pending.Count == 0)
                {
                    _log($"BBDD al dia (version {lastVersion}).");
                    return true;
                }

                using NpgsqlTransaction transaction = connection.BeginTransaction();
                string lastCommand = string.Empty;
                try
                {
                    foreach (Migration migration in pending)
                    {
                        foreach (string sql in migration.Commands)
                        {
                            lastCommand = sql;
                            using NpgsqlCommand command = new NpgsqlCommand(sql, connection, transaction);
                            command.ExecuteNonQuery();
                        }
                    }

                    int newVersion = pending[pending.Count - 1].Version;
                    using (NpgsqlCommand updateVersion = new NpgsqlCommand($"UPDATE public.\"{_numberTableVersion}\" SET \"{_numberTableVersion}\" = @v;", connection, transaction))
                    {
                        updateVersion.Parameters.AddWithValue("v", newVersion);
                        updateVersion.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _log($"{pending.Count} migracion(es) aplicada(s). Version {lastVersion} -> {newVersion}.");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _log("ERROR aplicando migraciones. Ultimo comando: " + lastCommand + Environment.NewLine + ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log("ERROR inesperado: " + ex);
                return false;
            }
        }
        #endregion

        #region _openWithRetry
        private static void _openWithRetry(NpgsqlConnection connection)
        {
            int attempt = 0;
            while (connection.State != ConnectionState.Open)
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    if (attempt >= maximumNumberOfConnectionRetries)
                        throw;

                    _log($"reintentando conexion ({attempt + 1}/{maximumNumberOfConnectionRetries}). {ex.Message}");
                    Thread.Sleep(timeWaitRetryMs);
                }
                attempt++;
            }
        }
        #endregion

        #region _log
        private static void _log(string message) => Console.WriteLine("DatabaseMigrator -> " + message);
        #endregion
    }
}
