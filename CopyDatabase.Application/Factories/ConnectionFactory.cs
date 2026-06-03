using CopyDatabase.MsSQLServer;

using System.Data.Common;

namespace CopyDatabase.Core.Factories;
public sealed class DbConnectionFactory : IDbConnectionFactory {

    /// <summary>
    /// Gets the database connection for the given provider.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public DbConnection GetDbConnection(SecureString connectionString, DatabaseProvider provider) {
        return provider switch {
            DatabaseProvider.MsSQLServer => ConnectionHelper.GetSqlConnection(connectionString.FromSecureString()),
            _ => throw new NotImplementedException(),
        };
    }
}
