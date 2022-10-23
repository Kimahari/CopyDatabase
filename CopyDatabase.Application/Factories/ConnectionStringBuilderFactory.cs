using CopyDatabase.Common;
using CopyDatabase.MsSQLServer;
using System.Data;

namespace CopyDatabase.Core.Factories;
internal class ConnectionStringBuilderFactory {
    public static IConnectionStringBuilder GetConnectionStringBuilder() {
        return new MsSQLConnectionStringBuilder();
    }
}
