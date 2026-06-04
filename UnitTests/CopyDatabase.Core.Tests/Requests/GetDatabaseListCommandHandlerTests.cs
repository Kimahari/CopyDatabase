namespace CopyDatabase.Core.Requests.Tests;

public sealed class GetDatabaseListCommandHandlerTests
{
    private readonly FakeDatabaseCatalogProvider catalogProvider = new();
    private readonly GetDatabaseListCommandHandler sut;

    public GetDatabaseListCommandHandlerTests()
    {
        sut = new GetDatabaseListCommandHandler(Mock.Of<IDatabaseCatalogProviderFactory>(
            oo => oo.GetDatabaseCatalogProvider(DatabaseProvider.MsSQLServer) == catalogProvider));
    }

    [Fact]
    public async Task ShouldGiveEmptyArrayWhenCredentialsIsNotProvided()
    {
        var result = await sut.Handle(new GetDatabaseList(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ShouldUseProviderCatalogToGetDatabaseListWhenCredentialsIsProvided()
    {
        catalogProvider.DatabaseNames = ["master", "CopyDatabaseSourceTest"];

        var result = await sut.Handle(new GetDatabaseList
        {
            ServerCredentials = new DatabaseServerTestCredentials { UseWindowsAuth = true, DataSource = "." },
        }, CancellationToken.None);

        Assert.Equal(["master", "CopyDatabaseSourceTest"], result);
        Assert.True(catalogProvider.DatabaseNamesRequested);
    }

    private sealed class FakeDatabaseCatalogProvider : IDatabaseCatalogProvider
    {
        public string[] DatabaseNames { get; set; } = [];
        public bool DatabaseNamesRequested { get; private set; }

        public Task<string[]> GetDatabaseNamesAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken)
        {
            DatabaseNamesRequested = true;
            return Task.FromResult(DatabaseNames);
        }

        public Task<string[]> GetSchemaObjectNamesAsync(
            IDatabaseServerCredentials credentials,
            string databaseName,
            SchemaObjectType objectType,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
