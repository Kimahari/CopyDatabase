using CopyDatabase.MsSQLServer;
using System.Data.Common;

namespace CopyDatabase.Core.Factories; 
internal class ConnectionFactory {
    public static DbConnection GetDbConnection(SecureString connectionString) {
        return ConnectionHelper.GetSqlConnection(connectionString.FromSecureString());
    }
}
