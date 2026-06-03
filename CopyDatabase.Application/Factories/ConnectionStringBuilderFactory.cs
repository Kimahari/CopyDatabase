using CopyDatabase.Common;
using CopyDatabase.MsSQLServer;

using System.Data;

namespace CopyDatabase.Core.Factories;
internal class ConnectionStringBuilderFactory : IConnectionStringBuilderFactory {

    public IConnectionStringBuilder GetConnectionStringBuilder(DatabaseProvider databaseProvider) {
        return databaseProvider switch {
            DatabaseProvider.MsSQLServer => new MsSQLConnectionStringBuilder(),
            _ => throw new NotImplementedException()
        };
    }
}
