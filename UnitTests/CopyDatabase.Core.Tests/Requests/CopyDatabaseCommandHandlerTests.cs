using CopyDatabase.Core.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.Core.Tests.Requests;

public sealed class CopyDatabaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesRequestToExecutor()
    {
        var executor = new Mock<IDatabaseCopyExecutor>();
        var executorFactory = new Mock<IDatabaseCopyExecutorFactory>();
        var request = new CopyDatabaseRequest
        {
            SourceCredentials = Mock.Of<IDatabaseServerCredentials>(),
            DestinationCredentials = Mock.Of<IDatabaseServerCredentials>(),
            Provider = DatabaseProvider.MsSQLServer,
            DatabaseName = "CopyDatabaseSourceTest",
            DestinationDatabaseName = "CopyDatabaseRenamedTest",
            CopySchema = true,
            CopyData = true,
            DropDestinationDatabase = true
        };

        executorFactory
            .Setup(oo => oo.GetDatabaseCopyExecutor(DatabaseProvider.MsSQLServer))
            .Returns(executor.Object);

        var handler = new CopyDatabaseCommandHandler(executorFactory.Object);

        await handler.Handle(request, CancellationToken.None);

        executorFactory.Verify(oo => oo.GetDatabaseCopyExecutor(DatabaseProvider.MsSQLServer), Times.Once);
        executor.Verify(oo => oo.CopyAsync(request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void RegisterCoreServices_ResolvesCopyExecutorFactoryUsingKeyedProviderAdapter()
    {
        var services = new ServiceCollection();
        var expectedExecutor = Mock.Of<IDatabaseCopyExecutor>();
        services.RegisterCoreServices();
        services.AddKeyedSingleton<IDatabaseCopyExecutor>(DatabaseProvider.MsSQLServer, expectedExecutor);

        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IDatabaseCopyExecutorFactory>();
        var keyedExecutor = provider.GetRequiredKeyedService<IDatabaseCopyExecutor>(DatabaseProvider.MsSQLServer);
        var executor = factory.GetDatabaseCopyExecutor(DatabaseProvider.MsSQLServer);

        Assert.Same(keyedExecutor, executor);
        Assert.Empty(provider.GetServices<IDatabaseCopyExecutor>());
    }
}
