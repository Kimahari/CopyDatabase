using CopyDatabase.Common;

using System.Security;

namespace CopyDatabase.Core.Tests;

internal sealed class DatabaseServerTestCredentials : IDatabaseServerCredentials
{
    public bool UseWindowsAuth { get; set; } = false;

    public string DataSource { get; set; } = "";
    public string UserName { get; set; } = "";
    public SecureString Password { get; set; } = new();
}
