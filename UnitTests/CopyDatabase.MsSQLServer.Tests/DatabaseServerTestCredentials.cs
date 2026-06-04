using System.Security;

namespace CopyDatabase.MsSQLServer.Tests;

internal sealed class DatabaseServerTestCredentials : IDatabaseServerCredentials
{
    public bool UseWindowsAuth { get; set; }

    public string DataSource { get; set; } = "";
    public string UserName { get; set; } = "";
    public SecureString Password { get; set; } = new();
}
