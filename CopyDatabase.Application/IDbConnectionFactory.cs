using System.Data.Common;

namespace CopyDatabase.Core;

/// <summary>
/// Contract to define a database connection factory.
/// </summary>
public interface IDbConnectionFactory {

    /// <summary>
    /// Creates a database connection for the given provider and connection string.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    DbConnection GetDbConnection(SecureString connectionString, DatabaseProvider provider);
}