using CopyDatabase.Common;
using CopyDatabase.Core.Requests;
using CopyDatabase.MsSQLServer.Requests.SqlServer;

using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.MsSQLServer;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterSqlServerServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStringBuilder, MsSQLConnectionStringBuilder>();
        services.AddKeyedSingleton<IDatabaseCatalogProvider, SqlServerDatabaseCatalogProvider>(DatabaseProvider.MsSQLServer);
        services.AddKeyedSingleton<IDatabaseConnectionTester, SqlServerConnectionTester>(DatabaseProvider.MsSQLServer);
        services.AddSingleton<SqlServerDatabaseCatalog>();
        services.AddSingleton<SqlServerObjectScripter>();
        services.AddSingleton<SqlServerDataCopier>();
        services.AddKeyedSingleton<IDatabaseCopyExecutor, SqlServerDatabaseCopyExecutor>(DatabaseProvider.MsSQLServer);
        return services;
    }
}
