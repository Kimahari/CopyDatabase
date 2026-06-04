using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.MsSQLServer.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void RegisterSqlServerServices_RegistersKeyedProviderAdapters()
    {
        var services = new ServiceCollection();
        services.RegisterSqlServerServices();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredKeyedService<IDatabaseCatalogProvider>(DatabaseProvider.MsSQLServer));
        Assert.NotNull(provider.GetRequiredKeyedService<IDatabaseConnectionTester>(DatabaseProvider.MsSQLServer));
        Assert.NotNull(provider.GetRequiredKeyedService<IDatabaseCopyExecutor>(DatabaseProvider.MsSQLServer));
        Assert.Empty(provider.GetServices<IDatabaseCopyExecutor>());
    }
}
