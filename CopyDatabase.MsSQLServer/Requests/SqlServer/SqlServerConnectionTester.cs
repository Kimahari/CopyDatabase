using CopyDatabase.Common;
using CopyDatabase.Core.Requests;

using Microsoft.Data.SqlClient;

using System.Data;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerConnectionTester : IDatabaseConnectionTester
{
    private readonly IConnectionStringBuilder connectionStringBuilder;

    public SqlServerConnectionTester(IConnectionStringBuilder connectionStringBuilder)
    {
        this.connectionStringBuilder = connectionStringBuilder;
    }

    public async Task<bool> TestConnectionAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken)
    {
        string connectionString = connectionStringBuilder.BuildConnection(credentials).FromSecureString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == ConnectionState.Open;
    }
}
