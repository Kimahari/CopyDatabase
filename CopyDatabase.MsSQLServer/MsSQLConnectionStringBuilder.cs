using CopyDatabase.Common;
using System.Security;
using System.Text;

namespace CopyDatabase.MsSQLServer;

public sealed class MsSQLConnectionStringBuilder : IConnectionStringBuilder {
    public SecureString BuildConnection(IDatabaseServerCredentials credentials, string databaseName = "") {
        string intergrated = credentials.UseWindowsAuth ? "SSPI" : "False";

        StringBuilder builder = new($"Data Source={credentials.DataSource};Integrated Security={intergrated};");

        if (!string.IsNullOrEmpty(credentials.UserName)) {
            builder.Append($"User ID={credentials.UserName};Password={credentials.Password.FromSecureString()};");
        }

        if (!string.IsNullOrEmpty(databaseName)) {
            builder.Append($"Initial Catalog={databaseName};");
        }

        return builder.ToString().ToSecureString();
    }
}
